# =========================================================
# Módulo ECR
#
# Crea un repositorio por imagen, con política de lifecycle
# (retener las últimas N imágenes) y escaneo bajo demanda.
# =========================================================

resource "aws_ecr_repository" "this" {
  for_each = toset(var.repositories)

  name                 = each.value
  image_tag_mutability = var.image_tag_mutability
  force_delete         = var.force_delete

  image_scanning_configuration {
    scan_on_push = var.scan_on_push
  }

  tags = merge(var.tags, {
    Name = each.value
  })
}

resource "aws_ecr_lifecycle_policy" "this" {
  for_each = toset(var.repositories)

  repository = aws_ecr_repository.this[each.value].name

  policy = jsonencode({
    rules = [
      {
        rulePriority = 1
        description  = "Keep last ${var.lifecycle_max_images} images"
        selection = {
          tagStatus   = "any"
          countType   = "imageCountMoreThan"
          countNumber = var.lifecycle_max_images
        }
        action = {
          type = "expire"
        }
      }
    ]
  })
}
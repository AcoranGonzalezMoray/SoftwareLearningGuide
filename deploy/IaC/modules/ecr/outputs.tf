output "repository_names" {
  description = "Nombres de los repositorios ECR creados."
  value       = [for repo in aws_ecr_repository.this : repo.name]
}

output "repository_urls" {
  description = "Mapa { nombre => repository_url } listo para docker tag/push."
  value = {
    for repo in aws_ecr_repository.this : repo.name => repo.repository_url
  }
}

output "repository_arns" {
  description = "ARNs de los repositorios ECR."
  value = {
    for repo in aws_ecr_repository.this : repo.name => repo.arn
  }
}

output "docker_tags" {
  description = "Comandos docker tag para las imágenes del sistema."
  value = [
    for repo in aws_ecr_repository.this :
    "docker tag softwarelearningguide/<repo>:local ${repo.repository_url}:latest"
  ]
}
# =========================================================
# Software Learning Guide — main.tf
# Orquesta los tres módulos: VPC, ECR y EKS.
# =========================================================

provider "aws" {
  region                      = var.region
  access_key                  = var.aws_access_key
  secret_key                  = var.aws_secret_key
  skip_credentials_validation = var.aws_skip_credentials_validation
  skip_requesting_account_id  = var.aws_skip_requesting_account_id
  skip_metadata_api_check     = true

  # Si aws_minstack_endpoint está relleno, TODOS los servicios AWS apuntan
  # a MiniStack (emulador local) en vez de a AWS real.
  dynamic "endpoints" {
    for_each = var.aws_minstack_endpoint != "" ? [1] : []

    content {
      ec2        = var.aws_minstack_endpoint
      sts        = var.aws_minstack_endpoint
      iam        = var.aws_minstack_endpoint
      ecr        = var.aws_minstack_endpoint
      eks        = var.aws_minstack_endpoint
      s3         = var.aws_minstack_endpoint
      logs       = var.aws_minstack_endpoint
      cloudwatch = var.aws_minstack_endpoint
      cognitoidp = var.aws_minstack_endpoint
    }
  }

  default_tags {
    tags = merge(var.tags, {
      Project     = var.project_name
      Environment = var.environment
    })
  }
}

locals {
  # Prefijo único para dar nombre a los recursos: softwarelearningguide-dev
  name_prefix = "${var.project_name}-${var.environment}"
}

# ---------------------------------------------------------
# VPC — red aislada con subredes públicas/privadas
# ---------------------------------------------------------
module "vpc" {
  source = "./modules/vpc"

  name_prefix        = local.name_prefix
  cidr               = var.vpc_cidr
  azs                = var.availability_zones
  enable_nat_gateway = var.enable_nat_gateway
  single_nat_gateway = var.single_nat_gateway
  enable_flow_logs   = var.enable_vpc_flow_logs
  tags               = var.tags
}

# ---------------------------------------------------------
# ECR — repositorios de imágenes que consumen los charts Helm
# ---------------------------------------------------------
module "ecr" {
  source = "./modules/ecr"

  repositories         = var.ecr_repositories
  image_tag_mutability = var.ecr_image_tag_mutability
  scan_on_push         = var.ecr_scan_on_push
  lifecycle_max_images = var.ecr_lifecycle_max_images
  force_delete         = var.ecr_force_delete
  tags                 = var.tags
}

# ---------------------------------------------------------
# EKS — clúster donde se instalan los charts Helm
# ---------------------------------------------------------
module "eks" {
  source = "./modules/eks"

  cluster_name                 = "${local.name_prefix}-eks"
  cluster_version              = var.eks_cluster_version
  vpc_id                       = module.vpc.vpc_id
  subnet_ids                   = module.vpc.private_subnet_ids
  node_desired_size            = var.eks_node_desired_size
  node_min_size                = var.eks_node_min_size
  node_max_size                = var.eks_node_max_size
  node_instance_types          = var.eks_node_instance_types
  node_disk_size               = var.eks_node_disk_size
  node_capacity_type           = var.eks_node_capacity_type
  enable_cluster_logging       = var.eks_enable_cluster_logging
  endpoint_private_access      = var.eks_endpoint_private_access
  endpoint_public_access       = var.eks_endpoint_public_access
  endpoint_public_access_cidrs = var.eks_endpoint_public_access_cidrs
  cluster_addons               = var.eks_cluster_addons
  admin_arns                   = var.eks_admin_arns
  enable_access_config         = var.eks_enable_access_config
  create_node_group            = var.eks_create_managed_node_group
  create_addons                = var.eks_create_addons
  create_access_entries        = var.eks_create_access_entries
  create_oidc_provider         = var.eks_create_oidc_provider
  tags                         = var.tags
}
# =========================================================
# Software Learning Guide — Outputs
# Valores que se necesitan después del apply (kubeconfig,
# URLs de ECR, ids de red, provider OIDC para IRSA, ...).
# =========================================================

# ---------------------------------------------------------
# VPC
# ---------------------------------------------------------

output "vpc_id" {
  description = "Id de la VPC creada."
  value       = module.vpc.vpc_id
}

output "public_subnet_ids" {
  description = "Ids de las subredes públicas (publicación/balancer de la API)."
  value       = module.vpc.public_subnet_ids
}

output "private_subnet_ids" {
  description = "Ids de las subredes privadas (nodos EKS)."
  value       = module.vpc.private_subnet_ids
}

# ---------------------------------------------------------
# ECR
# ---------------------------------------------------------

output "ecr_repository_names" {
  description = "Nombres de los repositorios ECR creados."
  value       = module.ecr.repository_names
}

output "ecr_repository_urls" {
  description = "URL de cada repositorio ECR. Úsala para etiquetar y subir las imágenes: docker tag/push."
  value       = module.ecr.repository_urls
}

output "ecr_docker_tags" {
  description = "Comandos docker tag listos para cada imagen (formato <account>.dkr.ecr.<region>.amazonaws.com/<repo>:latest)."
  value       = module.ecr.docker_tags
}

# ---------------------------------------------------------
# EKS
# ---------------------------------------------------------

output "eks_cluster_name" {
  description = "Nombre del clúster EKS."
  value       = module.eks.cluster_name
}

output "eks_cluster_endpoint" {
  description = "Endpoint del API server del clúster EKS."
  value       = module.eks.cluster_endpoint
}

output "eks_cluster_certificate_authority" {
  description = "Autoridad certificadora del clúster (necesaria para kubectl/helm)."
  value       = module.eks.cluster_certificate_authority
  sensitive   = true
}

output "eks_cluster_security_group_id" {
  description = "Security group gestionado por EKS para el plano de control."
  value       = module.eks.cluster_security_group_id
}

output "eks_node_group_name" {
  description = "Nombre del managed node group."
  value       = module.eks.node_group_name
}

output "eks_oidc_provider_arn" {
  description = "ARN del OIDC provider (para ServiceAccounts de IRSA)."
  value       = module.eks.oidc_provider_arn
}

output "eks_oidc_provider_url" {
  description = "URL del OIDC provider (para ServiceAccounts de IRSA)."
  value       = module.eks.oidc_provider_url
}

output "kubeconfig_command" {
  description = "Comando para obtener el kubeconfig del clúster y empezar a usar kubectl/helm."
  value = format(
    "aws eks --region %s update-kubeconfig --name %s --alias %s",
    var.region,
    module.eks.cluster_name,
    module.eks.cluster_name,
  )
}
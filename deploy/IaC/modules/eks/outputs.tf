output "cluster_name" {
  description = "Nombre del clúster EKS."
  value       = aws_eks_cluster.this.name
}

output "cluster_endpoint" {
  description = "Endpoint del API server del clúster."
  value       = aws_eks_cluster.this.endpoint
}

output "cluster_certificate_authority" {
  description = "Base64 del certificado del CA del clúster (usado por kubectl)."
  value       = aws_eks_cluster.this.certificate_authority[0].data
}

output "cluster_security_group_id" {
  description = "Security group gestionado por EKS para el plano de control."
  value       = aws_eks_cluster.this.vpc_config[0].cluster_security_group_id
}

output "node_group_name" {
  description = "Nombre del managed node group (vacío si create_node_group=false)."
  value       = var.create_node_group ? aws_eks_node_group.main[0].node_group_name : ""
}

output "node_role_arn" {
  description = "ARN del IAM Role asociado a los nodos."
  value       = aws_iam_role.node.arn
}

output "oidc_provider_arn" {
  description = "ARN del OIDC provider (vacío si create_oidc_provider=false)."
  value       = var.create_oidc_provider ? aws_iam_openid_connect_provider.cluster[0].arn : ""
}

output "oidc_provider_url" {
  description = "URL del OIDC provider (vacío si create_oidc_provider=false)."
  value       = var.create_oidc_provider ? aws_iam_openid_connect_provider.cluster[0].url : ""
}
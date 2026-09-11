# =========================================================
# Software Learning Guide — Variables globales
# Todas las variables se pueden sobreescribir con una
# línea `variable = valor` en un archivo *.tfvars
# (ver terraform.tfvars.example) o con `-var` en el CLI.
# =========================================================

# ---------------------------------------------------------
# General
# ---------------------------------------------------------

variable "project_name" {
  description = "Nombre del proyecto; se usa como prefijo de los recursos creados."
  type        = string
  default     = "softwarelearningguide"
}

variable "environment" {
  description = "Entorno de despliegue (dev, staging, prod)."
  type        = string
  default     = "dev"
}

variable "region" {
  description = "Región AWS por defecto para todos los recursos."
  type        = string
  default     = "us-east-1"
}

variable "tags" {
  description = "Etiquetas comunes aplicadas a todos los recursos (se fusionan con project/environment)."
  type        = map(string)
  default = {
    Project   = "softwarelearningguide"
    ManagedBy = "terraform"
  }
}

# ---------------------------------------------------------
# Provider AWS / emulación con MiniStack
# ---------------------------------------------------------

variable "aws_access_key" {
  description = "AWS Access Key. Vacío = cadena de credenciales habitual (env, shared credentials, etc.). Con MiniStack usa 'test'."
  type        = string
  default     = ""
}

variable "aws_secret_key" {
  description = "AWS Secret Key. Vacío = cadena de credenciales habitual. Con MiniStack usa 'test'."
  type        = string
  default     = ""
}

variable "aws_minstack_endpoint" {
  description = "Cuando se rellena (http://localhost:4566) TODOS los servicios del provider apuntan a MiniStack en vez de a AWS real."
  type        = string
  default     = ""
}

variable "aws_skip_credentials_validation" {
  description = "No llama a STS para validar credenciales. Evita el fallo de plan/apply sin credenciales reales (MiniStack/local)."
  type        = bool
  default     = true
}

variable "aws_skip_requesting_account_id" {
  description = "No resuelve el Account ID con STS GetCallerIdentity. Evita llamadas a AWS real durante plan/apply (MiniStack/local)."
  type        = bool
  default     = true
}

# ---------------------------------------------------------
# VPC
# ---------------------------------------------------------

variable "availability_zones" {
  description = "Zonas de disponibilidad usadas para las subredes públicas y privadas."
  type        = list(string)
  default     = ["us-east-1a", "us-east-1b", "us-east-1c"]
}

variable "vpc_cidr" {
  description = "Rango CIDR de la VPC."
  type        = string
  default     = "10.0.0.0/16"
}

variable "enable_nat_gateway" {
  description = "Crea un NAT Gateway por zona de disponibilidad (true) o ninguno (false)."
  type        = bool
  default     = true
}

variable "single_nat_gateway" {
  description = "Usa un único NAT Gateway compartido por todas las AZs (reduce coste, añade un punto único de fallo)."
  type        = bool
  default     = false
}

variable "enable_vpc_flow_logs" {
  description = "Habilita VPC Flow Logs enviados a CloudWatch Logs (auditoría de tráfico de red)."
  type        = bool
  default     = true
}

# ---------------------------------------------------------
# ECR
# ---------------------------------------------------------

variable "ecr_repositories" {
  description = "Nombres de los repositorios ECR que se crean (uno por imagen del sistema)."
  type        = list(string)
  default = [
    "softwarelearningguide-api",
    "softwarelearningguide-outboxprocessor",
    "softwarelearningguide-consumer",
  ]
}

variable "ecr_image_tag_mutability" {
  description = "MUTABLE para desarrollo, IMMUTABLE para producción (no se puede sobrescribir una etiqueta)."
  type        = string
  default     = "MUTABLE"
}

variable "ecr_scan_on_push" {
  description = "Escanea la imagen en busca de vulnerabilidades en cada push (EBS/EBS default)."
  type        = bool
  default     = true
}

variable "ecr_lifecycle_max_images" {
  description = "Número máximo de imágenes retenidas por repositorio según la política de lifecycle."
  type        = number
  default     = 10
}

variable "ecr_force_delete" {
  description = "Permite borrar el repositorio ECR aunque contenga imágenes (útil para `terraform destroy` en dev)."
  type        = bool
  default     = true
}

# ---------------------------------------------------------
# EKS
# ---------------------------------------------------------

variable "eks_cluster_version" {
  description = "Versión de Kubernetes del plano de control de EKS. Ajusta según lo soportado en tu cuenta (aws eks describe-versions)."
  type        = string
  default     = "1.31"
}

variable "eks_node_desired_size" {
  description = "Número deseado de nodos del managed node group."
  type        = number
  default     = 2
}

variable "eks_node_min_size" {
  description = "Número mínimo de nodos (límite inferior del autoscaling del node group)."
  type        = number
  default     = 2
}

variable "eks_node_max_size" {
  description = "Número máximo de nodos (el CA de EKS escala hasta este valor)."
  type        = number
  default     = 6
}

variable "eks_node_instance_types" {
  description = "Tipos de instancia EC2 de los nodos (EKS elige entre los que soporten todas las AZs)."
  type        = list(string)
  default     = ["t3.medium"]
}

variable "eks_node_disk_size" {
  description = "Tamaño en GB del disco EBS de cada nodo."
  type        = number
  default     = 50
}

variable "eks_node_capacity_type" {
  description = "ON_DEMAND o SPOT para los nodos."
  type        = string
  default     = "ON_DEMAND"
}

variable "eks_enable_cluster_logging" {
  description = "Control plane logging habilitado (api, audit, authenticator, controllerManager, scheduler). Vacío = sin logs."
  type        = list(string)
  default     = ["api", "audit", "authenticator", "controllerManager", "scheduler"]
}

variable "eks_admin_arns" {
  description = "ARNs de usuarios/roles IAM a los que se concederá AmazonEKSClusterAdminPolicy via Access Entries."
  type        = list(string)
  default     = []
}

variable "eks_endpoint_private_access" {
  description = "Habilita el endpoint privado del plano de control de EKS (recomendado en producción)."
  type        = bool
  default     = false
}

variable "eks_endpoint_public_access" {
  description = "Habilita el endpoint público del plano de control de EKS (necesario para kubectl desde tu máquina)."
  type        = bool
  default     = true
}

variable "eks_endpoint_public_access_cidrs" {
  description = "CIDRs permitidos contra el endpoint público del API server (liste su IP para producción)."
  type        = list(string)
  default     = ["0.0.0.0/0"]
}

variable "eks_cluster_addons" {
  description = "Add-ons de EKS instalados con el clúster."
  type        = list(string)
  default     = ["vpc-cni", "kube-proxy", "coredns"]
}

# Toggles para ejecuciones contra MiniStack/emulación local (ver la sección
# "Despliegue emulado con MiniStack" del README). En AWS real se dejan a true.

variable "eks_enable_access_config" {
  description = "Incluye access_config (API_AND_CONFIG_MAP) en el clúster."
  type        = bool
  default     = true
}

variable "eks_create_managed_node_group" {
  description = "Crea el managed node group. En MiniStack pon a false (el clúster embebido k3s ya tiene su nodo)."
  type        = bool
  default     = true
}

variable "eks_create_addons" {
  description = "Instala los add-ons de EKS. En MiniStack pon a false (k3s embebido los incluye)."
  type        = bool
  default     = true
}

variable "eks_create_access_entries" {
  description = "Crea Access Entries para los admin_arns. En MiniStack pon a false."
  type        = bool
  default     = true
}

variable "eks_create_oidc_provider" {
  description = "Crea el OIDC provider para IRSA. En MiniStack pon a false."
  type        = bool
  default     = true
}
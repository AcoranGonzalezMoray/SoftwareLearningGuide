variable "cluster_name" {
  description = "Nombre del clúster EKS."
  type        = string
}

variable "cluster_version" {
  description = "Versión de Kubernetes del clúster."
  type        = string
}

variable "vpc_id" {
  description = "Id de la VPC donde se crea el clúster."
  type        = string
}

variable "subnet_ids" {
  description = "Subredes (privadas) donde EKS crea el plano de control y los nodos."
  type        = list(string)
}

variable "node_desired_size" {
  description = "Nodos deseados."
  type        = number
  default     = 2
}

variable "node_min_size" {
  description = "Mínimo de nodos."
  type        = number
  default     = 2
}

variable "node_max_size" {
  description = "Máximo de nodos."
  type        = number
  default     = 6
}

variable "node_instance_types" {
  description = "Tipos de instancia para los nodos."
  type        = list(string)
  default     = ["t3.medium"]
}

variable "node_disk_size" {
  description = "Tamaño (GB) del disco EBS de los nodos."
  type        = number
  default     = 50
}

variable "node_capacity_type" {
  description = "ON_DEMAND o SPOT."
  type        = string
  default     = "ON_DEMAND"
}

variable "enable_cluster_logging" {
  description = "Tipos de log del control plane a habilitar."
  type        = list(string)
  default     = []
}

variable "endpoint_private_access" {
  description = "Endpoint privado del API server."
  type        = bool
  default     = false
}

variable "endpoint_public_access" {
  description = "Endpoint público del API server."
  type        = bool
  default     = true
}

variable "endpoint_public_access_cidrs" {
  description = "CIDRs permitidos en el endpoint público."
  type        = list(string)
  default     = ["0.0.0.0/0"]
}

variable "cluster_addons" {
  description = "Add-ons de EKS instalados (vpc-cni, kube-proxy, coredns, ...)."
  type        = list(string)
  default     = ["vpc-cni", "kube-proxy", "coredns"]
}

variable "admin_arns" {
  description = "ARNs de IAM a los que conceder administración del clúster (Access Entries)."
  type        = list(string)
  default     = []
}

variable "tags" {
  description = "Etiquetas comunes aplicadas a los recursos del clúster."
  type        = map(string)
  default     = {}
}

# ---------------------------------------------------------
# Toggles para entornos reducidos (MiniStack/emulación local):
# se pueden desactivar las partes que el emulador no soporta.
# ---------------------------------------------------------

variable "enable_access_config" {
  description = "Incluye el bloque access_config (API_AND_CONFIG_MAP) en el clúster. Pon a false si el emulador no soporta Access Entries."
  type        = bool
  default     = true
}

variable "create_node_group" {
  description = "Crea el managed node group (Auto Scaling). En MiniStack no existe: el cluster embebido (k3s) ya aporta su nodo."
  type        = bool
  default     = true
}

variable "create_addons" {
  description = "Instala los add-ons de EKS (vpc-cni, kube-proxy, coredns). En MiniStack no aplica: el k3s embebido los trae."
  type        = bool
  default     = true
}

variable "create_access_entries" {
  description = "Crea Access Entries para los admin_arns. En MiniStack déjalo a false (sin IAM de AWS real el k3s usa su propio auth)."
  type        = bool
  default     = true
}

variable "create_oidc_provider" {
  description = "Crea el OIDC provider para IRSA. En MiniStack déjalo a false (no hay issuer AWS real)."
  type        = bool
  default     = true
}
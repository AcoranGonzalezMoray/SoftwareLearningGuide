variable "repositories" {
  description = "Nombres de los repositorios ECR a crear."
  type        = list(string)
}

variable "image_tag_mutability" {
  description = "MUTABLE o IMMUTABLE."
  type        = string
  default     = "MUTABLE"
}

variable "scan_on_push" {
  description = "Escaneo de vulnerabilidades en cada push."
  type        = bool
  default     = true
}

variable "lifecycle_max_images" {
  description = "Número máximo de imágenes conservadas por repositorio."
  type        = number
  default     = 10
}

variable "force_delete" {
  description = "Permite eliminar repositorios con imágenes."
  type        = bool
  default     = true
}

variable "tags" {
  description = "Etiquetas comunes aplicadas a los repositorios."
  type        = map(string)
  default     = {}
}
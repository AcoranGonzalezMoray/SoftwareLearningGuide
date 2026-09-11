variable "name_prefix" {
  description = "Prefijo único para los nombres de los recursos de red."
  type        = string
}

variable "cidr" {
  description = "Rango CIDR de la VPC."
  type        = string
  default     = "10.0.0.0/16"
}

variable "azs" {
  description = "Zonas de disponibilidad donde se crean subredes públicas y privadas."
  type        = list(string)
}

variable "enable_nat_gateway" {
  description = "Crea NAT Gateways (true) o no (false)."
  type        = bool
  default     = true
}

variable "single_nat_gateway" {
  description = "Un único NAT Gateway compartido por todas las AZs."
  type        = bool
  default     = false
}

variable "enable_flow_logs" {
  description = "Habilita VPC Flow Logs hacia CloudWatch Logs."
  type        = bool
  default     = false
}

variable "flow_logs_retention_days" {
  description = "Días de retención del log group de Flow Logs."
  type        = number
  default     = 90
}

variable "tags" {
  description = "Etiquetas comunes aplicadas a los recursos de red."
  type        = map(string)
  default     = {}
}
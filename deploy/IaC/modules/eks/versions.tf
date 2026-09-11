terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.50"
    }
    # Usado para obtener el thumbprint del certificado del OIDC provider (IRSA)
    tls = {
      source  = "hashicorp/tls"
      version = "~> 4.0"
    }
  }
}
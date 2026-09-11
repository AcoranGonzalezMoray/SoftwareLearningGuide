terraform {
  required_version = ">= 1.5.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.50"
    }
  }

  # =========================================================
  # Estado remoto en S3 (opcional). Por defecto el estado se
  # guarda en ./terraform.tfstate (back end local, ideal para
  # el flujo de emulación local y para `plan` sin tocar AWS).
  #
  # Cuando vayas a desplegar en AWS real, descomenta el bloque
  # y crea el bucket + tabla de DynamoDB previamente:
  #
  #   aws s3api create-bucket --bucket slg-terraform-state --region us-east-1
  #   aws dynamodb create-table --table-name slg-terraform-locks ...
  #
  # backend "s3" {
  #   bucket         = "slg-terraform-state"
  #   key            = "softwarelearningguide/terraform.tfstate"
  #   region         = "us-east-1"
  #   dynamodb_table = "slg-terraform-locks"
  #   encrypt        = true
  # }
}
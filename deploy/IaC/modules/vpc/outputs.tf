output "vpc_id" {
  description = "Id de la VPC."
  value       = aws_vpc.this.id
}

output "vpc_cidr" {
  description = "Rango CIDR de la VPC."
  value       = aws_vpc.this.cidr_block
}

output "public_subnet_ids" {
  description = "Ids de las subredes públicas."
  value       = [for subnet in aws_subnet.public : subnet.id]
}

output "private_subnet_ids" {
  description = "Ids de las subredes privadas."
  value       = [for subnet in aws_subnet.private : subnet.id]
}

output "nat_gateway_ids" {
  description = "Ids de los NAT Gateways creados."
  value       = [for ngw in aws_nat_gateway.this : ngw.id]
}
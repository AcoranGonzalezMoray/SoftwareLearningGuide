# =========================================================
# Módulo VPC
#
# VPC multi-AZ con:
#  - 1 subred pública  por AZ (carga pública / NAT)
#  - 1 subred privada  por AZ (nodos EKS, workloads)
#  - Internet Gateway para las subredes públicas
#  - NAT Gateway (por AZ o único) para egress de las privadas
#  - Flow Logs opcionales hacia CloudWatch Logs
# =========================================================

locals {
  # Mapa { az => índice } con claves estables para for_each
  az_map = { for idx, az in var.azs : az => idx }

  # Número de NAT Gateways: 0 (desactivado) | 1 (single) | len(azs)
  nat_count = var.enable_nat_gateway ? (var.single_nat_gateway ? 1 : length(var.azs)) : 0
}

# ---------------------------------------------------------
# VPC + Internet Gateway
# ---------------------------------------------------------

resource "aws_vpc" "this" {
  cidr_block           = var.cidr
  enable_dns_support   = true
  enable_dns_hostnames = true

  tags = merge(var.tags, {
    Name = "${var.name_prefix}-vpc"
  })
}

resource "aws_internet_gateway" "this" {
  vpc_id = aws_vpc.this.id

  tags = merge(var.tags, {
    Name = "${var.name_prefix}-igw"
  })
}

# ---------------------------------------------------------
# Subredes
# ---------------------------------------------------------

resource "aws_subnet" "public" {
  for_each = local.az_map

  vpc_id                  = aws_vpc.this.id
  cidr_block              = cidrsubnet(var.cidr, 8, each.value * 2)
  availability_zone       = each.key
  map_public_ip_on_launch = true

  tags = merge(var.tags, {
    Name = "${var.name_prefix}-public-${each.key}"
    Tier = "public"
  })
}

resource "aws_subnet" "private" {
  for_each = local.az_map

  vpc_id            = aws_vpc.this.id
  cidr_block        = cidrsubnet(var.cidr, 8, each.value * 2 + 1)
  availability_zone = each.key

  tags = merge(var.tags, {
    Name = "${var.name_prefix}-private-${each.key}"
    Tier = "private"
  })
}

# ---------------------------------------------------------
# NAT Gateways (EIP + NAT por subnet pública)
# ---------------------------------------------------------

resource "aws_eip" "nat" {
  for_each = { for az, i in local.az_map : az => i if local.nat_count > 0 && (!var.single_nat_gateway || i == 0) }

  domain = "vpc"

  tags = merge(var.tags, {
    Name = "${var.name_prefix}-nat-${each.key}"
  })
}

resource "aws_nat_gateway" "this" {
  for_each = { for az, i in local.az_map : az => i if local.nat_count > 0 && (!var.single_nat_gateway || i == 0) }

  allocation_id = aws_eip.nat[each.key].id
  subnet_id     = aws_subnet.public[each.key].id

  tags = merge(var.tags, {
    Name = "${var.name_prefix}-nat-${each.key}"
  })
}

# ---------------------------------------------------------
# Route tables
# ---------------------------------------------------------

resource "aws_route_table" "public" {
  vpc_id = aws_vpc.this.id

  tags = merge(var.tags, {
    Name = "${var.name_prefix}-public"
  })
}

resource "aws_route" "public_igw" {
  route_table_id         = aws_route_table.public.id
  destination_cidr_block = "0.0.0.0/0"
  gateway_id             = aws_internet_gateway.this.id
}

resource "aws_route_table" "private" {
  for_each = local.az_map

  vpc_id = aws_vpc.this.id

  tags = merge(var.tags, {
    Name = "${var.name_prefix}-private-${each.key}"
  })
}

# Ruta por defecto hacia el NAT solo en las AZs que tengan NAT Gateway
resource "aws_route" "private_nat" {
  for_each = aws_nat_gateway.this

  route_table_id         = aws_route_table.private[each.key].id
  destination_cidr_block = "0.0.0.0/0"
  nat_gateway_id         = each.value.id
}

# ---------------------------------------------------------
# Asociaciones de subredes
# ---------------------------------------------------------

resource "aws_route_table_association" "public" {
  for_each = local.az_map

  subnet_id      = aws_subnet.public[each.key].id
  route_table_id = aws_route_table.public.id
}

resource "aws_route_table_association" "private" {
  for_each = local.az_map

  subnet_id      = aws_subnet.private[each.key].id
  route_table_id = aws_route_table.private[each.key].id
}

# ---------------------------------------------------------
# Flow Logs (opcional)
# ---------------------------------------------------------

resource "aws_cloudwatch_log_group" "flow_logs" {
  count = var.enable_flow_logs ? 1 : 0

  name              = "/aws/vpc/${var.name_prefix}-flow-logs"
  retention_in_days = var.flow_logs_retention_days

  tags = var.tags
}

resource "aws_iam_role" "flow_logs" {
  count = var.enable_flow_logs ? 1 : 0

  name = "${var.name_prefix}-flow-logs-role"
  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect    = "Allow"
      Principal = { Service = "vpc-flow-logs.amazonaws.com" }
      Action    = "sts:AssumeRole"
    }]
  })

  tags = var.tags
}

resource "aws_iam_role_policy" "flow_logs" {
  count = var.enable_flow_logs ? 1 : 0

  name = "${var.name_prefix}-flow-logs-policy"
  role = aws_iam_role.flow_logs[count.index].id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect   = "Allow"
      Action   = ["logs:CreateLogGroup", "logs:CreateLogStream", "logs:PutLogEvents", "logs:DescribeLogGroups", "logs:DescribeLogStreams"]
      Resource = "*"
    }]
  })
}

resource "aws_flow_log" "this" {
  count = var.enable_flow_logs ? 1 : 0

  iam_role_arn    = aws_iam_role.flow_logs[count.index].arn
  log_destination = aws_cloudwatch_log_group.flow_logs[count.index].arn
  traffic_type    = "ALL"
  vpc_id          = aws_vpc.this.id

  tags = merge(var.tags, {
    Name = "${var.name_prefix}-flow-logs"
  })
}
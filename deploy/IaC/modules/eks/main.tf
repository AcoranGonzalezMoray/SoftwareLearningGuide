# =========================================================
# Módulo EKS
#
# Crea:
#  - IAM Roles del clúster y de los nodos
#  - Clúster EKS (control plane) + Access Entries modernos
#  - Managed Node Group con autoscaling (desired/min/max)
#  - Add-ons: vpc-cni, kube-proxy, coredns
#  - OIDC provider para IRSA (ServiceAccounts ↔ IAM)
# =========================================================

# Nota: los ARNs de políticas AWS usan la partición "aws".
# Si desplegaras en GovCloud/China deberías derivarla de data.aws_partition.

# ---------------------------------------------------------
# IAM Roles
# ---------------------------------------------------------

resource "aws_iam_role" "cluster" {
  name = "${var.cluster_name}-cluster-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect    = "Allow"
      Principal = { Service = "eks.amazonaws.com" }
      Action    = "sts:AssumeRole"
    }]
  })

  tags = var.tags
}

resource "aws_iam_role_policy_attachment" "cluster_AmazonEKSClusterPolicy" {
  policy_arn = "arn:aws:iam::aws:policy/AmazonEKSClusterPolicy"
  role       = aws_iam_role.cluster.name
}

resource "aws_iam_role" "node" {
  name = "${var.cluster_name}-node-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect    = "Allow"
      Principal = { Service = "ec2.amazonaws.com" }
      Action    = "sts:AssumeRole"
    }]
  })

  tags = var.tags
}

resource "aws_iam_role_policy_attachment" "node_AmazonEKSWorkerNodePolicy" {
  policy_arn = "arn:aws:iam::aws:policy/AmazonEKSWorkerNodePolicy"
  role       = aws_iam_role.node.name
}

resource "aws_iam_role_policy_attachment" "node_AmazonEKS_CNI_Policy" {
  policy_arn = "arn:aws:iam::aws:policy/AmazonEKS_CNI_Policy"
  role       = aws_iam_role.node.name
}

resource "aws_iam_role_policy_attachment" "node_AmazonEC2ContainerRegistryReadOnly" {
  policy_arn = "arn:aws:iam::aws:policy/AmazonEC2ContainerRegistryReadOnly"
  role       = aws_iam_role.node.name
}

# ---------------------------------------------------------
# Clúster EKS
# ---------------------------------------------------------

resource "aws_eks_cluster" "this" {
  name     = var.cluster_name
  role_arn = aws_iam_role.cluster.arn
  version  = var.cluster_version

  vpc_config {
    subnet_ids              = var.subnet_ids
    endpoint_private_access = var.endpoint_private_access
    endpoint_public_access  = var.endpoint_public_access
    public_access_cidrs     = var.endpoint_public_access_cidrs
  }

  enabled_cluster_log_types = var.enable_cluster_logging

  # Habilita el modelo moderno de Access Entries (además de configmap aws-auth).
  # Opcional: en MiniStack el emulador de EKS usa su propio auth (k3s) y no soporta
  # access_config; pon enable_access_config=false en esa ejecución.
  dynamic "access_config" {
    for_each = var.enable_access_config ? [1] : []
    content {
      authentication_mode = "API_AND_CONFIG_MAP"
      # bootstrap_cluster_creator_admin_permissions por defecto es true:
      # el principal que ejecuta Terraform recibe AmazonEKSClusterAdminPolicy.
    }
  }

  depends_on = [
    aws_iam_role_policy_attachment.cluster_AmazonEKSClusterPolicy,
  ]

  tags = merge(var.tags, {
    Name = var.cluster_name
  })
}

# ---------------------------------------------------------
# Add-ons del clúster
# ---------------------------------------------------------

resource "aws_eks_addon" "this" {
  for_each = var.create_addons ? toset(var.cluster_addons) : toset([])

  cluster_name = aws_eks_cluster.this.name
  addon_name   = each.value

  # OVERWRITE en creación y PRESERVE en actualización evita bloqueos
  # en clusteres gestionados por Terraform (argumentos modernos).
  resolve_conflicts_on_create = "OVERWRITE"
  resolve_conflicts_on_update = "PRESERVE"

  depends_on = [aws_eks_cluster.this]
}

# ---------------------------------------------------------
# Managed Node Group
# ---------------------------------------------------------

resource "aws_eks_node_group" "main" {
  count = var.create_node_group ? 1 : 0

  cluster_name    = aws_eks_cluster.this.name
  node_group_name = "${var.cluster_name}-nodes"

  node_role_arn  = aws_iam_role.node.arn
  subnet_ids     = var.subnet_ids
  instance_types = var.node_instance_types
  disk_size      = var.node_disk_size
  capacity_type  = var.node_capacity_type

  scaling_config {
    desired_size = var.node_desired_size
    min_size     = var.node_min_size
    max_size     = var.node_max_size
  }

  update_config {
    max_unavailable = 1
  }

  depends_on = [
    aws_iam_role_policy_attachment.node_AmazonEKSWorkerNodePolicy,
    aws_iam_role_policy_attachment.node_AmazonEKS_CNI_Policy,
    aws_iam_role_policy_attachment.node_AmazonEC2ContainerRegistryReadOnly,
  ]

  tags = merge(var.tags, {
    "kubernetes.io/cluster/${var.cluster_name}" = "owned"
    Name                                        = "${var.cluster_name}-nodes"
  })
}

# ---------------------------------------------------------
# Access Entries: administradores del clúster
# (sustituye a la lambda de aws-auth; autenticación moderna)
# ---------------------------------------------------------

resource "aws_eks_access_entry" "admin" {
  for_each = var.create_access_entries ? toset(var.admin_arns) : toset([])

  cluster_name  = aws_eks_cluster.this.name
  principal_arn = each.value
  type          = "STANDARD"
}

resource "aws_eks_access_policy_association" "admin" {
  for_each = var.create_access_entries ? toset(var.admin_arns) : toset([])

  cluster_name  = aws_eks_cluster.this.name
  principal_arn = each.value
  policy_arn    = "arn:aws:eks::aws:cluster-access-policy/AmazonEKSClusterAdminPolicy"

  access_scope {
    type = "cluster"
  }

  depends_on = [aws_eks_access_entry.admin]
}

# ---------------------------------------------------------
# OIDC Provider (IRSA)
# ---------------------------------------------------------

data "tls_certificate" "cluster" {
  count = var.create_oidc_provider ? 1 : 0
  url   = aws_eks_cluster.this.identity[0].oidc[0].issuer
}

resource "aws_iam_openid_connect_provider" "cluster" {
  count           = var.create_oidc_provider ? 1 : 0
  client_id_list  = ["sts.amazonaws.com"]
  thumbprint_list = [data.tls_certificate.cluster[0].certificates[0].sha1_fingerprint]
  url             = aws_eks_cluster.this.identity[0].oidc[0].issuer
}
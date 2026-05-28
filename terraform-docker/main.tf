# ─────────────────────────────────────────────────────────────────────────────
# IMÁGENES (se bajan de Docker Hub automáticamente)
# ─────────────────────────────────────────────────────────────────────────────
resource "docker_image" "gateway" {
  name         = "${var.dockerhub_username}/itm-tickets-gateway:${var.image_tag}"
  keep_locally = false
}

resource "docker_image" "inventory" {
  name         = "${var.dockerhub_username}/itm-tickets-inventory:${var.image_tag}"
  keep_locally = false
}

resource "docker_container" "order" {
  name  = "itm-order"
  image = docker_image.order.image_id

  ports {
    internal = 8080
    external = 5110
  }

  env = [
    "ASPNETCORE_ENVIRONMENT=Production",
    "InventoryClient__BaseAddress=http://itm-inventory:8080",
    "PriceClient__BaseAddress=http://itm-price:8080"
  ]

  networks_advanced {
    name = docker_network.itm_network.name
  }
}

resource "docker_container" "price" {
  name  = "itm-price"
  image = docker_image.price.image_id

  ports {
    internal = 8080
    external = 5022
  }

  env = [
    "ASPNETCORE_ENVIRONMENT=Production",
    "ConnectionStrings__Redis=itm-redis:6379"
  ]

  networks_advanced {
    name = docker_network.itm_network.name
  }
}

resource "docker_container" "search" {
  name  = "itm-search"
  image = docker_image.search.image_id

  ports {
    internal = 8080
    external = 5062
  }

  env = [
    "ASPNETCORE_ENVIRONMENT=Production",
    "Elasticsearch__Uri=http://itm-elasticsearch:9200"
  ]

  networks_advanced {
    name = docker_network.itm_network.name
  }
}

# ─────────────────────────────────────────────────────────────────────────────
# RED INTERNA (los contenedores se comunican entre sí)
# ─────────────────────────────────────────────────────────────────────────────
resource "docker_network" "itm_network" {
  name = "itm-tickets-network"
}

# ─────────────────────────────────────────────────────────────────────────────
# CONTENEDORES
# ─────────────────────────────────────────────────────────────────────────────
resource "docker_container" "gateway" {
  name  = "itm-gateway"
  image = docker_image.gateway.image_id

  ports {
    internal = 8080
    external = 5183
  }

  env = [
    "ASPNETCORE_ENVIRONMENT=Production"
  ]

  networks_advanced {
    name = docker_network.itm_network.name
  }
}

resource "docker_container" "inventory" {
  name  = "itm-inventory"
  image = docker_image.inventory.image_id

  ports {
    internal = 8080
    external = 5273
  }

  env = [
    "ASPNETCORE_ENVIRONMENT=Production"
  ]

  networks_advanced {
    name = docker_network.itm_network.name
  }
}

resource "docker_container" "order" {
  name  = "itm-order"
  image = docker_image.order.image_id

  ports {
    internal = 8080
    external = 5110
  }

  env = [
    "ASPNETCORE_ENVIRONMENT=Production"
  ]

  networks_advanced {
    name = docker_network.itm_network.name
  }
}

resource "docker_container" "price" {
  name  = "itm-price"
  image = docker_image.price.image_id

  ports {
    internal = 8080
    external = 5022
  }

  env = [
    "ASPNETCORE_ENVIRONMENT=Production"
  ]

  networks_advanced {
    name = docker_network.itm_network.name
  }
}

resource "docker_container" "search" {
  name  = "itm-search"
  image = docker_image.search.image_id

  ports {
    internal = 8080
    external = 5062
  }

  env = [
    "ASPNETCORE_ENVIRONMENT=Production"
  ]

  networks_advanced {
    name = docker_network.itm_network.name
  }
}

# ─────────────────────────────────────────────────────────────────────────────
# OUTPUTS (info útil al hacer terraform apply)
# ─────────────────────────────────────────────────────────────────────────────
output "gateway_url" {
  value = "http://localhost:5183"
}

output "inventory_url" {
  value = "http://localhost:5273"
}

output "order_url" {
  value = "http://localhost:5110"
}

output "price_url" {
  value = "http://localhost:5022"
}

output "search_url" {
  value = "http://localhost:5062"
}

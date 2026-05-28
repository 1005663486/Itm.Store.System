# ─────────────────────────────────────────────────────────────────────────────
# IMÁGENES
# ─────────────────────────────────────────────────────────────────────────────
resource "docker_image" "gateway" {
  name         = "${var.dockerhub_username}/itm-tickets-gateway:${var.image_tag}"
  keep_locally = false
}

resource "docker_image" "inventory" {
  name         = "${var.dockerhub_username}/itm-tickets-inventory:${var.image_tag}"
  keep_locally = false
}

resource "docker_image" "order" {
  name         = "${var.dockerhub_username}/itm-tickets-order:${var.image_tag}"
  keep_locally = false
}

resource "docker_image" "price" {
  name         = "${var.dockerhub_username}/itm-tickets-price:${var.image_tag}"
  keep_locally = false
}

resource "docker_image" "search" {
  name         = "${var.dockerhub_username}/itm-tickets-search:${var.image_tag}"
  keep_locally = false
}

# ─────────────────────────────────────────────────────────────────────────────
# RED INTERNA
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
    "Redis__ConnectionString=itm-redis:6379"
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
resource "docker_image" "elasticsearch" {
  name         = "docker.elastic.co/elasticsearch/elasticsearch:8.13.4"
  keep_locally = true
}

resource "docker_container" "elasticsearch" {
  name  = "itm-elasticsearch"
  image = docker_image.elasticsearch.image_id

  env = [
    "discovery.type=single-node",
    "xpack.security.enabled=false",
    "ES_JAVA_OPTS=-Xms512m -Xmx512m"
  ]

  ports {
    internal = 9200
    external = 9200
  }

  networks_advanced {
    name = docker_network.itm_network.name
  }
}
resource "docker_image" "redis" {
  name         = "redis:latest"
  keep_locally = true
}

resource "docker_container" "redis" {
  name  = "itm-redis"
  image = docker_image.redis.image_id

  ports {
    internal = 6379
    external = 6379
  }

  networks_advanced {
    name = docker_network.itm_network.name
  }
}

# ─────────────────────────────────────────────────────────────────────────────
# OUTPUTS
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

# 1. Definimos la Imagen (Terraform la bajará de Docker Hub si no existe)
resource "docker_image" "nginx" {
  name         = "nginx:latest"  # Imagen oficial de Nginx en Docker Hub
  keep_locally = false         # No mantener la imagen si borramos el recurso
}

# 2. Definimos el Contenedor
resource "docker_container" "nginx_server" {
  image = docker_image.nginx.image_id # Hacemos referencia a la imagen de arriba
  name  = var.container_name          # Usamos la variable

  # Mapeo de puertos: Host -> Contenedor
  ports {
	internal = 80             # Puerto interno de Nginx (no cambiar)
	external = var.external_port # Puerto externo en tu PC (usamos la variable)
  }
}
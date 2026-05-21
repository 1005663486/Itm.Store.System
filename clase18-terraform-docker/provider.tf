# Definimos los requerimientos mínimos de Terraform y el proveedor
terraform {
  required_providers {
	docker = {
	  source  = "kreuzwerker/docker"
	  version = "~> 3.0" # Usar una versión mayor o igual a 3.0
	}
  }
}

# Configuramos el proveedor de Docker
provider "docker" {
  # Por defecto intenta conectarse al socket local de Docker. 
  # Funciona en Windows/Mac (Docker Desktop) y Linux.
}
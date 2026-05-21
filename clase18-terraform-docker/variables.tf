variable "container_name" {
  type        = string
  description = "Nombre para el contenedor de Nginx"
  default     = "itm-tickets-frontend"
}

variable "external_port" {
  type        = number
  description = "Puerto en la máquina host (tu PC) para acceder al contenedor"
  default     = 8080
}
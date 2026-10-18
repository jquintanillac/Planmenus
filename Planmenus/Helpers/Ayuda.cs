namespace Planmenus.Helpers
{
    /// <summary>
    /// Texto de ayuda de cada pantalla (boton "?" del encabezado).
    /// Se busca por "Controlador/Accion" y, si no existe, por "Controlador".
    /// </summary>
    public static class Ayuda
    {
        public record Tema(string Titulo, string Que, string[] Requisitos, string[] Recomendaciones);

        private static readonly Dictionary<string, Tema> Temas = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Home/Inicio"] = new("Inicio",
                "Resumen del día: ventas cobradas, tickets, ticket promedio, anulados, platos más vendidos y ventas por tipo de pago y por hora.",
                new[] { "Las cifras de venta solo se muestran a roles con permiso de Cuadre de caja o Lista de tickets." },
                new[] { "Revise los platos programados antes de abrir: sin menú del día no se puede vender.",
                        "Compare las ventas con las de ayer para detectar días flojos y planificar compras." }),

            ["Home/AccesoDenegado"] = new("Acceso denegado",
                "Su rol no tiene permiso para la opción que intentó abrir.",
                new[] { "El administrador asigna permisos en Administración → Roles Permisos." },
                new[] { "Después de que le den el permiso, cierre sesión y vuelva a ingresar para que se aplique." }),

            ["Configuracion"] = new("Configuración",
                "Datos del negocio que aparecen en tickets, cuadres y carta digital; formato del ticket; ticketera de red y qué tipos de pago son efectivo.",
                new[] { "El nombre del negocio es obligatorio.",
                        "El RUC, si se ingresa, debe tener 11 dígitos.",
                        "La IP de la ticketera solo funciona si la aplicación corre en una PC del mismo local que la impresora." },
                new[] { "Use “Vista previa del ticket” para revisar cómo saldrá antes de imprimir.",
                        "Si la app está en internet (hosting), deje la IP vacía e imprima desde el navegador; para no ver el diálogo de impresión abra Chrome o Edge con --kiosk-printing.",
                        "Marque como efectivo solo los pagos que entran a la gaveta: así el arqueo de caja cuadra." }),

            ["MDGenerales"] = new("Datos generales",
                "Registro con los datos de la empresa. Se recomienda editarlos desde Configuración, que reúne todo en una sola pantalla.",
                new[] { "Solo se usa el primer registro." },
                new[] { "Prefiera Administración → Configuración." }),

            ["MDUsuarios"] = new("Usuarios",
                "Personas que pueden ingresar al sistema. Cada usuario necesita un rol (Rol Usuarios) para ver opciones.",
                new[] { "El nombre de usuario es único.",
                        "La contraseña se guarda cifrada: nadie puede verla, solo cambiarla." },
                new[] { "Cree un usuario por persona; así el ticket y la caja muestran quién atendió.",
                        "Desactive (no elimine) a quien deje de trabajar: se conserva su historial.",
                        "Al editar, deje la contraseña en blanco para mantener la actual." }),

            ["MDRols"] = new("Roles",
                "Perfiles de trabajo (Administrador, Cajero, Mozo, Cocina…). Los permisos se asignan al rol, no a la persona.",
                new[] { "Un rol inactivo no permite ingresar a sus usuarios." },
                new[] { "Cree pocos roles y claros, por ejemplo: Cajero (venta, salón, caja), Mozo (venta, salón), Cocina (solo cocina).",
                        "Luego asigne permisos en Roles Permisos." }),

            ["MDRol_Permisos"] = new("Permisos por rol",
                "Marque qué opciones del menú puede usar cada rol. Las pantallas sin permiso quedan ocultas y bloqueadas.",
                new[] { "Seleccione el rol arriba antes de marcar.",
                        "Los cambios se aplican cuando el usuario vuelve a iniciar sesión." },
                new[] { "Dé a cada rol solo lo que necesita: por ejemplo, el mozo no necesita reportes ni anular tickets." }),

            ["MDRol_Usuario"] = new("Rol de cada usuario",
                "Asigna a cada usuario el rol con el que trabaja.",
                new[] { "Un usuario sin rol no puede iniciar sesión." },
                new[] { "Asigne un solo rol por usuario para evitar confusiones." }),

            ["MDMenuprins"] = new("Módulos del menú",
                "Grupos principales del menú lateral (Administración, Planificación, Caja).",
                new[] { "Uso técnico: los permisos dependen de estos registros." },
                new[] { "No elimine ni cambie los existentes; solo edite la descripción si lo necesita." }),

            ["MDSubmenus"] = new("Submenús",
                "Opciones de cada módulo. Se usan para asignar permisos.",
                new[] { "Uso técnico: el sistema reconoce cada opción por su número." },
                new[] { "No elimine submenús existentes." }),

            ["MDMenus"] = new("Programación del menú",
                "Platos que se venden cada día con su precio. Solo los platos de hoy aparecen en la venta y en la carta digital.",
                new[] { "Cada plato necesita descripción, precio y fecha.",
                        "Un plato con ventas no se puede eliminar." },
                new[] { "Programe el menú de mañana con anticipación; el calendario le ayuda a verlo por semana.",
                        "Use la observación para acompañamientos (“con arroz y ensalada”): se muestra en la carta digital." }),

            ["MDMenus/Calendar"] = new("Calendario de menús",
                "Vista mensual de los platos programados.",
                new[] { "Los eventos se crean al registrar platos en Programación Menú." },
                new[] { "Úselo para no repetir platos muy seguido y planificar compras." }),

            ["MDTipo_pago"] = new("Tipos de pago",
                "Formas de pago aceptadas: efectivo, Yape, Plin, tarjeta…",
                new[] { "Solo los activos aparecen al cobrar." },
                new[] { "Marque “Es efectivo” solo en el pago que entra a la gaveta; se usa en el arqueo de caja.",
                        "Desactive en lugar de eliminar: los reportes antiguos siguen mostrando el nombre." }),

            ["MDDeliveries"] = new("Repartidores",
                "Personas o servicios que llevan pedidos a domicilio.",
                new[] { "Solo los activos aparecen en la venta." },
                new[] { "Registre también apps de reparto (Rappi, PedidosYa) para saber cuánto vende cada canal." }),

            ["MDMesas"] = new("Mesas",
                "Mesas del local. Aparecen en el Salón con su estado (libre u ocupada) y en la venta.",
                new[] { "El número de mesa ordena el plano del salón.", "Solo las activas se muestran." },
                new[] { "Use nombres que el personal reconozca rápido: “Mesa 4”, “Terraza 2”, “Barra”." }),

            ["MDEgresos"] = new("Egresos",
                "Salidas de dinero de la caja: compras al paso, pagos a proveedores, propinas.",
                new[] { "Descripción, monto y fecha." },
                new[] { "Registre cada salida en el momento: el arqueo resta los egresos del efectivo esperado." }),

            ["MDIngresos"] = new("Ingresos",
                "Entradas de dinero que no son ventas (por ejemplo, un aporte para sencillo).",
                new[] { "Descripción, monto y fecha." },
                new[] { "No registre ventas aquí: se cuentan solas al cobrar." }),

            ["MDInicials"] = new("Saldo inicial",
                "Monto con el que empieza la caja en el cuadre diario.",
                new[] { "Un registro por día." },
                new[] { "Si usa Apertura y cierre de caja, el fondo inicial se registra allí." }),

            ["MDCajas/Create"] = new("Nueva venta",
                "Arme el pedido tocando los platos. Puede cobrar en el momento (para llevar, mostrador) o enviarlo a cocina y cobrar después desde el Salón.",
                new[] { "Debe haber platos programados para hoy.",
                        "Para cobrar ahora, elija el tipo de pago.",
                        "Para “Enviar a cocina” elija la mesa; si la mesa ya tiene pedido, los platos se suman a ese pedido." },
                new[] { "El total lo calcula el sistema con los precios del menú.",
                        "Abra la caja al iniciar el turno para que el arqueo cuadre.",
                        "Anote pedidos especiales en Observación (“sin cebolla”): se ven en cocina y en el ticket." }),

            ["MDCajas/Index"] = new("Cuadre de caja",
                "Genera el PDF con el resumen por día: saldo inicial, ventas, ingresos, egresos y total.",
                new[] { "Elija el rango de fechas." },
                new[] { "Para el control por turno y cajero use Apertura y cierre de caja." }),

            ["MDCajas/Cuadre"] = new("Cuadre de caja",
                "Genera el PDF con el resumen por día: saldo inicial, ventas, ingresos, egresos y total.",
                new[] { "Elija el rango de fechas." },
                new[] { "Para el control por turno y cajero use Apertura y cierre de caja." }),

            ["MDCajas/Cajadiaria"] = new("Lista de tickets",
                "Todos los pedidos del rango de fechas con su detalle, estado (cobrado, por cobrar o anulado) y quién lo registró.",
                new[] { "Anular requiere este permiso." },
                new[] { "Anule en lugar de eliminar: queda registro y el motivo.",
                        "Use “Imprimir” para reimprimir un ticket." }),

            ["MDCajas/Edit"] = new("Anular ticket",
                "Marca el ticket como anulado: deja de sumar en reportes y caja, pero queda en el historial.",
                new[] { "Escriba el motivo de la anulación." },
                new[] { "Anule solo con autorización del encargado; el motivo queda guardado." }),

            ["MDCajas/RptListpagos"] = new("Resumen por tipo de pago",
                "Cantidad de tickets y monto por tipo de pago y día.",
                new[] { "Elija el rango de fechas." },
                new[] { "Compárelo con los depósitos de Yape, Plin o tarjeta para conciliar." }),

            ["MDCajas/Rpttipodepago"] = new("Ventas por tipo de pago",
                "Detalle de cada ticket cobrado con su tipo de pago.",
                new[] { "Elija el rango de fechas." },
                new[] { "Use los botones de exportar para enviarlo a su contador." }),

            ["MDCajas/Rptporplato"] = new("Resumen por plato",
                "Unidades vendidas y monto por plato y día.",
                new[] { "Elija el rango de fechas." },
                new[] { "Identifique los platos estrella y los que casi no salen para ajustar la carta." }),

            ["MDCajas/Rptcantplato"] = new("Ventas por plato",
                "Cada plato vendido con su ticket.",
                new[] { "Elija el rango de fechas." },
                new[] { "Útil para revisar un ticket puntual." }),

            ["MDCajas"] = new("Caja",
                "Operaciones de venta y caja.",
                Array.Empty<string>(),
                new[] { "Use el menú Caja para ir a cada opción." }),

            ["Salon/Index"] = new("Salón y mesas",
                "Plano del salón: verde = libre, naranja = ocupada (con total, tiempo y estado en cocina). Toque una mesa libre para tomar el pedido o una ocupada para verlo, agregar platos o cobrar.",
                new[] { "Las mesas se crean en Planificación → Mesas.",
                        "Los pedidos “para llevar” pendientes aparecen abajo." },
                new[] { "Cobre desde aquí cuando el cliente pida la cuenta; puede imprimir la pre-cuenta antes.",
                        "Si una mesa lleva mucho tiempo, revise con cocina." }),

            ["Salon/Pedido"] = new("Pedido de mesa",
                "Detalle del pedido abierto: agregar o quitar platos, imprimir pre-cuenta y cobrar.",
                new[] { "Para cobrar elija el tipo de pago.", "Quitar platos solo es posible antes de cobrar." },
                new[] { "Imprima la pre-cuenta para que el cliente revise antes de pagar.",
                        "Si el cliente se retira sin consumir, anule el pedido desde Lista de tickets." }),

            ["Cocina/Index"] = new("Cocina",
                "Comandas pendientes en orden de llegada. Se actualiza sola cada 15 segundos. El color indica la espera: verde, ámbar (más de 10 min) y rojo (más de 20 min).",
                new[] { "Marque “Listo” cuando el pedido esté para servir y “Entregado” cuando salga." },
                new[] { "Use una tablet o TV en la cocina con esta pantalla abierta y un usuario con rol Cocina.",
                        "Si se agregan platos a un pedido, vuelve a aparecer como pendiente." }),

            ["Turnos/Index"] = new("Apertura y cierre de caja",
                "Abra la caja al empezar con el fondo inicial y ciérrela al terminar contando el efectivo. El sistema calcula lo esperado (fondo + ventas en efectivo + ingresos − egresos) y la diferencia.",
                new[] { "Solo puede haber una caja abierta a la vez.",
                        "Marque en Configuración qué tipos de pago son efectivo." },
                new[] { "Cuente el dinero antes de mirar el esperado para un arqueo honesto.",
                        "Cobre o anule los pedidos abiertos antes de cerrar.",
                        "Explique en la observación cualquier diferencia." }),

            ["Turnos/Detalle"] = new("Detalle del turno",
                "Resumen del turno: ventas por tipo de pago, ingresos, egresos, efectivo esperado, contado y diferencia.",
                Array.Empty<string>(),
                new[] { "Imprima esta pantalla y archívela con el dinero entregado." }),

            ["Carta/Qr"] = new("Carta digital (QR)",
                "Código QR que lleva a la carta pública con los platos de hoy y sus precios. Se actualiza sola al programar el menú.",
                new[] { "La carta usa los datos del negocio de Configuración." },
                new[] { "Imprima el QR y péguelo en cada mesa y en la puerta.",
                        "Compártalo en WhatsApp y redes para recibir pedidos." }),
        };

        public static Tema Buscar(string controlador, string accion)
        {
            if (Temas.TryGetValue($"{controlador}/{accion}", out var tema) || Temas.TryGetValue(controlador ?? "", out tema))
            {
                return tema;
            }
            return null;
        }
    }
}

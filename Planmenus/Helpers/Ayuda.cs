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
                "Datos del negocio que aparecen en tickets, cuadres y carta digital; formato del ticket y ticketera; qué tipos de pago son efectivo; y avisos por correo de las reservas.",
                new[] { "El nombre del negocio es obligatorio.",
                        "El RUC, si se ingresa, debe tener 11 dígitos.",
                        "La IP de la ticketera solo funciona si la aplicación corre en una PC del mismo local que la impresora." },
                new[] { "Use “Vista previa del ticket” para revisar cómo saldrá antes de imprimir.",
                        "Si la app está en internet (hosting), deje la IP vacía e imprima desde el navegador; para no ver el diálogo de impresión abra Chrome o Edge con --kiosk-printing.",
                        "Marque como efectivo solo los pagos que entran a la gaveta: así el arqueo de caja cuadra." }),

            ["Ticketera/Index"] = new("Ticketera",
                "Configure cómo se imprimen los tickets: la conexión con su ticketera, el ancho del papel, el logo, qué datos lleva el ticket, el corte, las copias y la apertura de la gaveta. La vista previa muestra cómo saldrá.",
                new[] { "Agente en la PC de caja: instale el Agente de impresión en esa PC y elija la impresora por su nombre de Windows.",
                        "Ticketera de red: solo funciona si el sistema corre en el mismo local que la ticketera.",
                        "Desde el navegador: funciona con cualquier impresora, pero muestra el diálogo de impresión." },
                new[] { "Si tiene una ticketera USB, use el modo Agente: imprime sin diálogos, corta el papel y abre la gaveta.",
                        "Si las tildes o la ñ salen mal, cambie el “Juego de caracteres”.",
                        "Use un logo con fondo blanco y ajuste el contraste hasta que la vista previa se vea nítida.",
                        "Imprima una prueba después de cada cambio." }),

            ["Facturacion/Index"] = new("Facturación electrónica",
                "Active la emisión de boletas y facturas electrónicas con envío directo a SUNAT (SEE - Del contribuyente). Configure el emisor, las credenciales SOL, el certificado digital, las series y la tasa de IGV.",
                new[] { "RUC válido, razón social y domicilio fiscal tal como figuran en SUNAT.",
                        "Certificado digital (.pfx o .p12) con su contraseña; en producción debe estar registrado en SUNAT.",
                        "En producción: usuario SOL secundario con perfil de emisión electrónica.",
                        "Las series de boletas empiezan con B y las de facturas con F." },
                new[] { "Empiece en el ambiente de pruebas: genere el certificado de prueba y envíe la boleta de prueba.",
                        "Confirme con su contador la tasa de IGV (restaurantes MYPE pueden tener tasa especial) y si debe usar un OSE.",
                        "Revise que el certificado no esté por vencer: sin él no se pueden emitir comprobantes." }),

            ["Comprobantes/Index"] = new("Comprobantes electrónicos",
                "Boletas, facturas y notas de crédito emitidas con su estado ante SUNAT. Descargue el XML firmado y la constancia (CDR), reenvíe los que no llegaron y anule con nota de crédito.",
                new[] { "Solo se pueden anular comprobantes aceptados; la anulación genera una nota de crédito.",
                        "Los comprobantes con error de envío se reintentan solos cada 10 minutos." },
                new[] { "Revise esta pantalla al cerrar el día: no deben quedar comprobantes por enviar.",
                        "Use el botón ZIP para enviarle a su contador los XML y CDR del mes." }),

            ["Comprobantes/Emitir"] = new("Emitir comprobante",
                "Emite una boleta o factura para una venta que se cobró con ticket interno.",
                new[] { "Factura: RUC y razón social del cliente.", "Boleta desde S/ 700: DNI y nombre del cliente." },
                new[] { "Emítala el mismo día de la venta." }),

            ["Ticketera/Muestra"] = new("Ticket de prueba",
                "Ticket de ejemplo con la configuración guardada.",
                Array.Empty<string>(),
                new[] { "Si el diálogo muestra otra impresora, elija la ticketera y desactive márgenes y encabezados." }),

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
                "Plano del salón por zona: verde = libre, naranja = ocupada (total y tiempo), mostaza = lista para servir y lila punteado = reservada en la próxima hora y media. La inicial en la esquina es el mozo asignado. Toque una mesa libre para tomar el pedido o una ocupada para verlo, agregar platos o cobrar.",
                new[] { "El plano se arma en Carta y planificación → Plano del salón; los mozos se asignan en Planificar mozos.",
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

            ["Plano/Index"] = new("Plano del salón",
                "Arme el plano de cada ambiente: agregue mesas cuadradas, redondas o rectangulares, arrástrelas a su lugar, cambie su tamaño y giro, y defina cuántas personas caben (las sillas se dibujan solas).",
                new[] { "Guarde el plano después de mover mesas (el botón avisa si hay cambios sin guardar).",
                        "Una zona solo se puede eliminar cuando no tiene mesas." },
                new[] { "Cree una zona por ambiente: Salón, Terraza, 2.º piso, Barra.",
                        "Use la capacidad real de cada mesa: se usa para sugerir mesas en las reservas.",
                        "Con una mesa seleccionada, las flechas del teclado la mueven con precisión." }),

            ["Mozos/Index"] = new("Planificar mozos",
                "Defina qué mozo atiende cada mesa en una fecha. Los pedidos de esa mesa se registran a su nombre y su inicial aparece en el plano del salón.",
                new[] { "Los mozos son usuarios activos cuyo rol tiene permiso de Nueva venta o Salón y mesas." },
                new[] { "Use “Asignar toda la zona” para repartir rápido y revise el resumen de cubiertos para equilibrar la carga.",
                        "“Copiar del día anterior” ahorra tiempo cuando el equipo se repite." }),

            ["Reservas/Index"] = new("Reservas",
                "Calendario de reservas y eventos. Cada reserva se pinta según su estado: pendiente, confirmada, completada, no asistió o cancelada. Arrastre una reserva para cambiarle la hora o estírela para cambiar su duración.",
                new[] { "Una mesa no puede tener dos reservas activas que se crucen en el horario." },
                new[] { "Haga clic en un horario libre para crear una reserva en esa hora.",
                        "Registre el correo del cliente: recibe la confirmación y un recordatorio antes de llegar.",
                        "Al terminar el día marque “Llegó” o “No asistió” para medir la asistencia." }),

            ["Reservas/Create"] = new("Nueva reserva",
                "Registre los datos del cliente, el horario, cuántas personas vienen y qué mesas se le guardan.",
                new[] { "Nombre, fecha y hora, y número de personas son obligatorios.",
                        "Las mesas tachadas ya están reservadas en ese horario." },
                new[] { "Si la deja Confirmada se envía la confirmación por correo al guardar.",
                        "En “Otros interesados” agregue a quien organiza el evento si es distinto del cliente." }),

            ["Reservas/Edit"] = new("Reserva",
                "Modifique la reserva, cambie su estado y revise los correos que se le enviaron.",
                new[] { "Si cambia la fecha u hora, se vuelve a enviar la confirmación y el recordatorio." },
                new[] { "Cancele en lugar de borrar: el cliente recibe el aviso y queda el historial." }),

            ["Reservas/Guardar"] = new("Reserva",
                "Revise los datos marcados y vuelva a guardar.",
                Array.Empty<string>(),
                new[] { "Si la mesa está ocupada en ese horario, elija otra o cambie la hora." }),

            ["Registro/Bienvenida"] = new("Bienvenida",
                "Asistente inicial de su restaurante: datos del local, cuántas mesas tiene y sus primeros platos del día.",
                new[] { "Solo se crean los platos con nombre y precio mayor a cero." },
                new[] { "Puede omitirlo y completar todo después en Configuración, Plano del salón y Programación del menú.",
                        "Cree usuarios para su caja y sus mozos en Administración → Usuarios, con su propio rol." }),

            ["Plataforma"] = new("Plataforma",
                "Panel del dueño del sistema: todos los restaurantes registrados con sus usuarios, ventas de los últimos 30 días y último ingreso. Desde aquí se suspende o reactiva un restaurante, se cambia su plan o se crea uno nuevo.",
                new[] { "Solo lo ven los usuarios marcados como superadministrador.",
                        "Su propia empresa no se puede suspender." },
                new[] { "Al suspender un restaurante sus usuarios no pueden ingresar y las sesiones abiertas se cierran en menos de un minuto; sus datos se conservan.",
                        "Su carta pública también deja de mostrarse mientras esté suspendido." }),

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

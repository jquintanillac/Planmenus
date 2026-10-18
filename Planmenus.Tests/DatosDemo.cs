using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Data.Entities;
using Planmenus.Helpers;

namespace Planmenus.Tests
{
    /// <summary>
    /// Carga datos de ejemplo: 5 registros por opcion (la empresa queda en un solo registro).
    /// Solo agrega lo que falta, por lo que se puede ejecutar varias veces sin duplicar.
    /// Ejecutar con: dotnet test --filter Categoria=DatosDemo
    /// </summary>
    [Collection("App")]
    [Trait("Categoria", "DatosDemo")]
    public class DatosDemo
    {
        public const string ClaveDemo = "Demo2026!";
        private readonly AppFactory _app;

        public DatosDemo(AppFactory app)
        {
            _app = app;
        }

        [Fact]
        public async Task Cargar_datos_de_ejemplo()
        {
            await _app.Con(async db =>
            {
                var hoy = Fechas.HoyPeru();
                var ahora = Fechas.AhoraPeru();

                // ---- Empresa (un solo registro) ----
                if (!await db.Generales.AnyAsync())
                {
                    db.Generales.Add(new MDGenerales { gene_empre = "Restaurante Demo", gene_direc = "Av. Principal 123, Lima", gene_telef = "987 654 321", gene_recordatorio_horas = 2, gene_reserva_minutos = 120 });
                }

                // ---- Tipos de pago ----
                foreach (var (nombre, efectivo) in new[] { ("Efectivo", true), ("Yape", false), ("Plin", false), ("Tarjeta", false), ("Transferencia", false) })
                {
                    if (!await db.Tipo_Pagos.AnyAsync(t => t.tipago_desc == nombre))
                        db.Tipo_Pagos.Add(new MDTipo_pago { tipago_desc = nombre, tipago_act = true, tipago_efectivo = efectivo });
                }

                // ---- Repartidores ----
                foreach (var (cod, nombre, desc) in new[] { ("R01", "Juan Pérez", "Moto propia"), ("R02", "Pedro Quispe", "Bicicleta"), ("APP1", "Rappi", "App de reparto"), ("APP2", "PedidosYa", "App de reparto"), ("APP3", "DiDi Food", "App de reparto") })
                {
                    if (!await db.Deliveries.AnyAsync(d => d.deliv_nombcom == nombre))
                        db.Deliveries.Add(new MDDelivery { deliv_codig = cod, deliv_nombcom = nombre, deliv_desc = desc, deliv_act = true });
                }

                // ---- Zonas ----
                // Se completan hasta 5 zonas activas respetando las que ya creo el usuario
                int orden = (await db.Zonas.MaxAsync(z => (int?)z.zona_orden) ?? 0) + 1;
                foreach (var nombre in new[] { "Salón", "Terraza", "Segundo piso", "Barra", "Privado" })
                {
                    if (await db.Zonas.CountAsync(z => z.zona_act) + db.Zonas.Local.Count(z => z.id_zona == 0) >= 5) break;
                    if (await db.Zonas.AnyAsync(x => x.zona_desc == nombre)) continue;
                    db.Zonas.Add(new MDZona { zona_desc = nombre, zona_orden = orden++, zona_act = true });
                }
                await db.SaveChangesAsync();
                // Zona principal: la primera activa (el usuario puede haberla renombrado)
                var salon = await db.Zonas.Where(z => z.zona_act).OrderBy(z => z.zona_orden).ThenBy(z => z.id_zona).Select(z => z.id_zona).FirstAsync();

                // ---- Mesas (en el plano del salon) ----
                var mesasDemo = new[]
                {
                    (1, "Mesa 1", 2, FormaMesa.Cuadrada, 120, 120, 90, 90),
                    (2, "Mesa 2", 4, FormaMesa.Cuadrada, 340, 120, 100, 100),
                    (3, "Mesa 3", 4, FormaMesa.Redonda, 580, 120, 110, 110),
                    (4, "Mesa 4", 6, FormaMesa.Rectangular, 160, 420, 190, 100),
                    (5, "Mesa 5", 8, FormaMesa.Rectangular, 520, 420, 240, 110),
                };
                foreach (var (nro, desc, cap, forma, x, y, w, h) in mesasDemo)
                {
                    if (await db.Mesas.CountAsync(m => m.mesa_act && m.id_zona == salon) + db.Mesas.Local.Count(m => m.id_mesa == 0) >= 5) break;
                    if (await db.Mesas.AnyAsync(m => m.mesa_act && m.mesa_nro == nro)) continue;
                    db.Mesas.Add(new MDMesas { mesa_nro = nro, mesa_desc = desc, mesa_capacidad = cap, mesa_forma = forma, mesa_x = x, mesa_y = y, mesa_ancho = w, mesa_alto = h, mesa_act = true, id_zona = salon });
                }

                // ---- Menu del dia (con su evento de calendario) ----
                foreach (var (plato, precio, obs) in new[] { ("Lomo saltado", 22m, "Con papas fritas y arroz"), ("Ají de gallina", 18m, "Con arroz y huevo"), ("Ceviche mixto", 25m, "Con camote y choclo"), ("Arroz con pollo", 18m, "Con papa a la huancaína"), ("Chicha morada", 5m, "Vaso de 500 ml") })
                {
                    if (!await db.Menus.AnyAsync(m => m.menu_desc == plato && m.menu_fecha >= hoy && m.menu_fecha < hoy.AddDays(1)))
                    {
                        var m = new MDMenu { menu_desc = plato, menu_precio = precio, menu_observ = obs, menu_fecha = hoy.AddHours(12) };
                        db.Menus.Add(m);
                        await db.SaveChangesAsync();
                        db.Eventos.Add(new MDEventos { id_menu = m.id_menu, title = plato, StarDate = m.menu_fecha, EndDate = m.menu_fecha, Description = "Programacion", Location = "" });
                    }
                }

                // ---- Roles con sus permisos ----
                await db.SaveChangesAsync();
                var submenus = await db.Submenus.AsNoTracking().ToListAsync();
                int[] cajero = { 18, 19, 20, 21, 22, 23, 24, 25, 27, 31 }, mozo = { 18, 25, 28 }, cocina = { 26 };
                var supervisor = submenus.Where(s => s.id_menprin != 1).Select(s => s.id_submenu).ToArray();
                foreach (var (nombre, activos) in new[] { ("Cajero", cajero), ("Mozo", mozo), ("Cocina", cocina), ("Supervisor", supervisor) })
                {
                    if (await db.Rols.AnyAsync(r => r.rol_desc == nombre)) continue;
                    var rol = new MDRol { rol_desc = nombre, rol_act = true };
                    db.Rols.Add(rol);
                    await db.SaveChangesAsync();
                    db.Rol_Permisos.AddRange(submenus.Select(s => new MDRol_Permisos { id_rol = rol.id_rol, id_menprin = s.id_menprin, id_submenu = s.id_submenu, rolper_act = activos.Contains(s.id_submenu) }));
                }
                await db.SaveChangesAsync();

                // ---- Usuarios y su rol ----
                foreach (var (usuario, nombre, rolNombre) in new[] { ("caja1", "Carla Ramos", "Cajero"), ("mozo1", "Rosa Quispe", "Mozo"), ("mozo2", "Luis Torres", "Mozo"), ("cocina1", "Miguel Chávez", "Cocina") })
                {
                    if (await db.Usuarios.AnyAsync(u => u.Usua_user == usuario)) continue;
                    var hash = HashHelper.Hash(ClaveDemo);
                    var u = new MDUsuarios { Usua_user = usuario, Usua_nombres = nombre, Usua_apellidos = "", Usua_email = "", Usua_pass = hash.Password, Usua_Hash = hash.Salt, Usua_act = true };
                    db.Usuarios.Add(u);
                    await db.SaveChangesAsync();
                    db.Rol_Usuarios.Add(new MDRol_Usuario { IdUsuario = u.IdUsuario, id_rol = await db.Rols.Where(r => r.rol_desc == rolNombre).Select(r => r.id_rol).FirstAsync() });
                }
                await db.SaveChangesAsync();

                var mesas = await db.Mesas.Where(m => m.mesa_act && m.id_zona == salon).OrderBy(m => m.mesa_nro).Take(5).ToListAsync();
                var mozo1 = await db.Usuarios.Where(u => u.Usua_user == "mozo1").Select(u => u.IdUsuario).FirstAsync();
                var mozo2 = await db.Usuarios.Where(u => u.Usua_user == "mozo2").Select(u => u.IdUsuario).FirstAsync();

                // ---- Planificacion de mozos de hoy ----
                for (int i = 0; i < mesas.Count; i++)
                {
                    if (!await db.Asignaciones.AnyAsync(a => a.asig_fecha == hoy && a.id_mesa == mesas[i].id_mesa))
                        db.Asignaciones.Add(new MDAsignacion_Mozo { asig_fecha = hoy, id_mesa = mesas[i].id_mesa, IdUsuario = i < 3 ? mozo1 : mozo2 });
                }

                // ---- Movimientos de caja de los ultimos 5 dias ----
                for (int d = 0; d < 5; d++)
                {
                    var dia = hoy.AddDays(-d);
                    if (!await db.Inicial.AnyAsync(x => x.ini_fecha >= dia && x.ini_fecha < dia.AddDays(1)))
                        db.Inicial.Add(new MDInicial { ini_desc = "Fondo de caja", ini_monto = 100m, ini_fecha = dia.AddHours(9) });
                }
                foreach (var (desc, monto, dias) in new[] { ("Compra de hielo", 12m, 0), ("Gas", 45m, 1), ("Verduras del mercado", 80m, 2), ("Propina repartidor", 10m, 3), ("Bolsas y descartables", 25m, 4) })
                {
                    if (!await db.Egresos.AnyAsync(e => e.egre_descr == desc))
                        db.Egresos.Add(new MDEgresos { egre_descr = desc, egre_costo = monto, egre_fecha = hoy.AddDays(-dias).AddHours(11) });
                }
                foreach (var (desc, monto, dias) in new[] { ("Sencillo del banco", 50m, 0), ("Pago de evento (adelanto)", 200m, 1), ("Venta de botellas recicladas", 15m, 2), ("Devolución de proveedor", 30m, 3), ("Aporte para sencillo", 40m, 4) })
                {
                    if (!await db.Ingresos.AnyAsync(e => e.ingre_desc == desc))
                        db.Ingresos.Add(new MDIngresos { ingre_desc = desc, ingre_monto = monto, ingre_fecha = hoy.AddDays(-dias).AddHours(10) });
                }

                // ---- Turnos de caja cerrados (dias anteriores) ----
                if (await db.Turnos.CountAsync() < 5)
                {
                    for (int d = 5; d >= 1; d--)
                    {
                        var dia = hoy.AddDays(-d);
                        var esperado = 100m + 380m + 20m * d;
                        var contado = esperado - (d % 2 == 0 ? 0 : 2);
                        db.Turnos.Add(new MDTurno_Caja
                        {
                            turno_apertura = dia.AddHours(9), turno_cierre = dia.AddHours(22), turno_monto_inicial = 100m,
                            turno_usuario_apertura = "caja1", turno_usuario_cierre = "caja1",
                            turno_esperado = esperado, turno_contado = contado, turno_diferencia = contado - esperado,
                            turno_obs = contado == esperado ? "Cuadra" : "Faltan S/ 2 por vuelto", turno_abierto = false
                        });
                    }
                }
                await db.SaveChangesAsync();

                // ---- Ventas de hoy (cobradas) ----
                if (await db.Cajas.CountAsync(c => c.caja_fecha >= hoy) < 5)
                {
                    var menus = await db.Menus.Where(m => m.menu_fecha >= hoy && m.menu_fecha < hoy.AddDays(1)).ToDictionaryAsync(m => m.menu_desc, m => m);
                    var pagos = await db.Tipo_Pagos.ToDictionaryAsync(t => t.tipago_desc, t => t.id_tipago);
                    var ventas = new[]
                    {
                        (new[] { ("Lomo saltado", 2), ("Chicha morada", 2) }, "Efectivo", 0, "mozo1", 3),
                        (new[] { ("Ceviche mixto", 1) }, "Yape", 1, "mozo1", 2),
                        (new[] { ("Ají de gallina", 3), ("Chicha morada", 3) }, "Tarjeta", 3, "mozo2", 1),
                        (new[] { ("Arroz con pollo", 1) }, "Plin", -1, "caja1", 1),
                        (new[] { ("Lomo saltado", 1), ("Ceviche mixto", 1) }, "Efectivo", 2, "mozo1", 0),
                    };
                    foreach (var (platos, pago, mesa, mozoVenta, horasAtras) in ventas)
                    {
                        var momento = ahora.AddHours(-horasAtras).AddMinutes(-15);
                        var detalle = platos.Select(p => new MDCaja_Detalle { id_menu = menus[p.Item1].id_menu, det_cant = p.Item2, det_precio = menus[p.Item1].menu_precio, det_subtotal = menus[p.Item1].menu_precio * p.Item2 }).ToList();
                        var caja = new MDCaja
                        {
                            id_tipago = pagos[pago], id_mesa = mesa >= 0 && mesa < mesas.Count ? mesas[mesa].id_mesa : null,
                            caja_monto = detalle.Sum(d => d.det_subtotal), caja_fecha = momento, caja_fecha_pago = momento.AddMinutes(40),
                            caja_usuario = "caja1", caja_mozo = mozoVenta, caja_act = true, caja_pagado = true, caja_cocina = EstadoCocina.Entregado
                        };
                        db.Cajas.Add(caja);
                        await db.SaveChangesAsync();
                        detalle.ForEach(d => d.id_caja = caja.id_caja);
                        db.Caja_Detalles.AddRange(detalle);
                    }
                    await db.SaveChangesAsync();
                }

                // ---- Reservas proximas ----
                var reservas = new[]
                {
                    ("Ana Torres", "999111222", "ana@example.com", 1, 20, 4, TipoReserva.Mesa, EstadoReserva.Confirmada, 1, (string)null),
                    ("Familia Rojas", "988222333", null, 2, 13, 8, TipoReserva.Mesa, EstadoReserva.Confirmada, 4, "Silla para bebé"),
                    ("Carlos Mendoza", "977333444", null, 2, 21, 2, TipoReserva.Mesa, EstadoReserva.Pendiente, 0, null),
                    ("Cumpleaños de Lucía", "966444555", "lucia@example.com", 4, 19, 12, TipoReserva.Evento, EstadoReserva.Confirmada, 4, "Torta propia; decoración con globos"),
                    ("Empresa Andina SAC", "955555666", null, 6, 13, 6, TipoReserva.Mesa, EstadoReserva.Pendiente, 3, "Almuerzo de trabajo"),
                };
                foreach (var (cliente, tel, mail, dias, hora, personas, tipo, estado, mesa, notas) in reservas)
                {
                    if (await db.Reservas.AnyAsync(r => r.res_cliente == cliente)) continue;
                    var inicio = hoy.AddDays(dias).AddHours(hora);
                    var r = new MDReserva
                    {
                        res_cliente = cliente, res_telefono = tel, res_email = mail, res_inicio = inicio, res_fin = inicio.AddHours(tipo == TipoReserva.Evento ? 4 : 2),
                        res_personas = personas, res_tipo = tipo, res_estado = estado, res_notas = notas, res_usuario = "admin", res_creado = ahora,
                        // Datos de ejemplo: se marcan como avisados para no enviar correos reales
                        res_confirmacion_enviada = estado == EstadoReserva.Confirmada ? ahora : null
                    };
                    db.Reservas.Add(r);
                    await db.SaveChangesAsync();
                    if (mesa < mesas.Count) db.Reserva_Mesas.Add(new MDReserva_Mesa { id_reserva = r.id_reserva, id_mesa = mesas[mesa].id_mesa });
                    if (tipo == TipoReserva.Evento && mesa + 1 < mesas.Count) db.Reserva_Mesas.Add(new MDReserva_Mesa { id_reserva = r.id_reserva, id_mesa = mesas[mesa - 1].id_mesa });
                }
                await db.SaveChangesAsync();
            });

            // Verificacion: 5 registros por opcion
            await _app.Con(async db =>
            {
                Assert.Equal(1, await db.Generales.CountAsync());
                Assert.True(await db.Usuarios.CountAsync() >= 5);
                Assert.True(await db.Rols.CountAsync() >= 5);
                Assert.True(await db.Rol_Usuarios.CountAsync() >= 5);
                Assert.True(await db.Tipo_Pagos.CountAsync() >= 5);
                Assert.True(await db.Deliveries.CountAsync() >= 5);
                Assert.True(await db.Zonas.CountAsync(z => z.zona_act) >= 5);
                Assert.True(await db.Mesas.CountAsync(m => m.mesa_act) >= 5);
                Assert.True(await db.Menus.CountAsync() >= 5);
                Assert.True(await db.Egresos.CountAsync() >= 5);
                Assert.True(await db.Ingresos.CountAsync() >= 5);
                Assert.True(await db.Inicial.CountAsync() >= 5);
                Assert.True(await db.Turnos.CountAsync() >= 5);
                Assert.True(await db.Cajas.CountAsync() >= 5);
                Assert.True(await db.Reservas.CountAsync() >= 5);
                Assert.True(await db.Asignaciones.CountAsync() >= 5);
            });
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Planmenus.Data.Entities;
using Planmenus.Helpers;

namespace Planmenus.Tests
{
    /// <summary>
    /// Insertar, editar y eliminar en cada mantenimiento del sistema.
    /// Cada prueba usa registros marcados con "TEST-" y los elimina al terminar.
    /// </summary>
    [Collection("App")]
    public class MantenimientosTests
    {
        private readonly AppFactory _app;

        public MantenimientosTests(AppFactory app)
        {
            _app = app;
        }

        private static string Fecha(DateTime d) => d.ToString("yyyy-MM-ddTHH:mm");

        [Fact]
        public async Task Usuarios_insertar_editar_eliminar()
        {
            var c = _app.Cliente();
            var marca = Marca.Nueva().ToLowerInvariant();
            await c.PostOkAsync("/MDUsuarios/Create", "/MDUsuarios/Create",
                ("Usua_user", marca), ("Usua_nombres", "Prueba"), ("Usua_apellidos", "Automática"), ("Usua_email", "p@x.pe"), ("Usua_pass", "Clave123!"), ("Usua_act", "true"));
            var u = await _app.Con(db => db.Usuarios.AsNoTracking().SingleAsync(x => x.Usua_user == marca));
            Assert.NotEqual("Clave123!", u.Usua_pass);
            Assert.True(HashHelper.CheckHash("Clave123!", u.Usua_pass, u.Usua_Hash), "La contraseña debe guardarse con hash");

            // Editar sin escribir contraseña la conserva
            await c.PostOkAsync($"/MDUsuarios/Edit/{u.IdUsuario}", $"/MDUsuarios/Edit/{u.IdUsuario}",
                ("IdUsuario", u.IdUsuario), ("Usua_user", marca), ("Usua_nombres", "Editado"), ("Usua_apellidos", "Automática"), ("Usua_email", "p@x.pe"), ("Usua_pass", ""), ("Usua_act", "true"));
            var editado = await _app.Con(db => db.Usuarios.AsNoTracking().SingleAsync(x => x.IdUsuario == u.IdUsuario));
            Assert.Equal("Editado", editado.Usua_nombres);
            Assert.Equal(u.Usua_pass, editado.Usua_pass);

            await c.PostOkAsync($"/MDUsuarios/Delete/{u.IdUsuario}", $"/MDUsuarios/Delete/{u.IdUsuario}", ("id", u.IdUsuario));
            Assert.False(await _app.Con(db => db.Usuarios.AnyAsync(x => x.IdUsuario == u.IdUsuario)));
        }

        [Fact]
        public async Task Roles_y_permisos_insertar_editar_eliminar()
        {
            var c = _app.Cliente();
            var marca = Marca.Nueva();
            await c.PostOkAsync("/MDRols/Create", "/MDRols/Create", ("rol_desc", marca), ("rol_act", "true"));
            var rol = await _app.Con(db => db.Rols.AsNoTracking().SingleAsync(x => x.rol_desc == marca));

            await c.PostOkAsync($"/MDRols/Edit/{rol.id_rol}", $"/MDRols/Edit/{rol.id_rol}", ("id_rol", rol.id_rol), ("rol_desc", marca + "-E"), ("rol_act", "true"));
            Assert.Equal(marca + "-E", await _app.Con(db => db.Rols.Where(x => x.id_rol == rol.id_rol).Select(x => x.rol_desc).SingleAsync()));

            // Al abrir los permisos del rol nuevo se crean sus filas; luego se otorgan dos permisos
            await c.HtmlAsync($"/MDRol_Permisos?id_rol={rol.id_rol}");
            var filas = await _app.Con(db => db.Rol_Permisos.AsNoTracking().Where(p => p.id_rol == rol.id_rol).ToListAsync());
            Assert.True(filas.Count >= 22, "El rol nuevo debe tener una fila por submenú");
            var dos = filas.Where(p => p.id_submenu is 18 or 25).Select(p => p.id_rolper).ToList();
            var r = await c.PostAsync($"/MDRol_Permisos?id_rol={rol.id_rol}", "/MDRol_Permisos/otorgar", ("Idrol", rol.id_rol), ("menus", dos[0]), ("menus", dos[1]));
            Assert.True((int)r.StatusCode < 400, $"otorgar devolvió {(int)r.StatusCode}");
            Assert.Equal(2, await _app.Con(db => db.Rol_Permisos.CountAsync(p => p.id_rol == rol.id_rol && p.rolper_act)));

            await _app.Con(async db => { db.Rol_Permisos.RemoveRange(db.Rol_Permisos.Where(p => p.id_rol == rol.id_rol)); await db.SaveChangesAsync(); });
            await c.PostOkAsync($"/MDRols/Delete/{rol.id_rol}", $"/MDRols/Delete/{rol.id_rol}", ("id", rol.id_rol));
            Assert.False(await _app.Con(db => db.Rols.AnyAsync(x => x.id_rol == rol.id_rol)));
        }

        [Fact]
        public async Task Rol_de_usuario_insertar_editar_eliminar()
        {
            var c = _app.Cliente();
            var marca = Marca.Nueva();
            var (idUsuario, idRol1, idRol2) = await _app.Con(async db =>
            {
                var u = new MDUsuarios { Usua_user = marca.ToLowerInvariant(), Usua_nombres = "Prueba", Usua_pass = "x", Usua_Hash = "x", Usua_act = true };
                var r1 = new MDRol { rol_desc = marca + "-A", rol_act = true };
                var r2 = new MDRol { rol_desc = marca + "-B", rol_act = true };
                db.AddRange(u, r1, r2);
                await db.SaveChangesAsync();
                return (u.IdUsuario, r1.id_rol, r2.id_rol);
            });

            await c.PostOkAsync("/MDRol_Usuario/Create", "/MDRol_Usuario/Create", ("IdUsuario", idUsuario), ("id_rol", idRol1));
            var ru = await _app.Con(db => db.Rol_Usuarios.AsNoTracking().SingleAsync(x => x.IdUsuario == idUsuario));
            await c.PostOkAsync($"/MDRol_Usuario/Edit/{ru.id_roluser}", $"/MDRol_Usuario/Edit/{ru.id_roluser}", ("id_roluser", ru.id_roluser), ("IdUsuario", idUsuario), ("id_rol", idRol2));
            Assert.Equal(idRol2, await _app.Con(db => db.Rol_Usuarios.Where(x => x.id_roluser == ru.id_roluser).Select(x => x.id_rol).SingleAsync()));
            await c.PostOkAsync($"/MDRol_Usuario/Delete/{ru.id_roluser}", $"/MDRol_Usuario/Delete/{ru.id_roluser}", ("id", ru.id_roluser));
            Assert.False(await _app.Con(db => db.Rol_Usuarios.AnyAsync(x => x.id_roluser == ru.id_roluser)));

            await _app.Con(async db =>
            {
                db.Usuarios.Remove(await db.Usuarios.FindAsync(idUsuario));
                db.Rols.RemoveRange(db.Rols.Where(r => r.id_rol == idRol1 || r.id_rol == idRol2));
                await db.SaveChangesAsync();
            });
        }

        [Fact]
        public async Task Modulos_y_submenus_insertar_editar_eliminar()
        {
            var c = _app.Cliente();
            var marca = Marca.Nueva();
            await c.PostOkAsync("/MDMenuprins/Create", "/MDMenuprins/Create", ("menuprin_desc", marca), ("menuprin_act", "true"));
            var mod = await _app.Con(db => db.Menuprins.AsNoTracking().SingleAsync(x => x.menuprin_desc == marca));
            await c.PostOkAsync($"/MDMenuprins/Edit/{mod.id_menprin}", $"/MDMenuprins/Edit/{mod.id_menprin}", ("id_menprin", mod.id_menprin), ("menuprin_desc", marca + "-E"), ("menuprin_act", "true"));
            Assert.Equal(marca + "-E", await _app.Con(db => db.Menuprins.Where(x => x.id_menprin == mod.id_menprin).Select(x => x.menuprin_desc).SingleAsync()));

            await c.PostOkAsync("/MDSubmenus/Create", "/MDSubmenus/Create", ("id_menprin", mod.id_menprin), ("submenu_desc", marca + "-S"), ("submenu_nro", "1"));
            var sub = await _app.Con(db => db.Submenus.AsNoTracking().SingleAsync(x => x.submenu_desc == marca + "-S"));
            await c.PostOkAsync($"/MDSubmenus/Edit/{sub.id_submenu}", $"/MDSubmenus/Edit/{sub.id_submenu}", ("id_submenu", sub.id_submenu), ("id_menprin", mod.id_menprin), ("submenu_desc", marca + "-S2"), ("submenu_nro", "2"));
            Assert.Equal("2", await _app.Con(db => db.Submenus.Where(x => x.id_submenu == sub.id_submenu).Select(x => x.submenu_nro).SingleAsync()));
            await c.PostOkAsync($"/MDSubmenus/Delete/{sub.id_submenu}", $"/MDSubmenus/Delete/{sub.id_submenu}", ("id", sub.id_submenu));
            await c.PostOkAsync($"/MDMenuprins/Delete/{mod.id_menprin}", $"/MDMenuprins/Delete/{mod.id_menprin}", ("id", mod.id_menprin));
            Assert.False(await _app.Con(db => db.Menuprins.AnyAsync(x => x.id_menprin == mod.id_menprin)));
        }

        [Fact]
        public async Task Menu_del_dia_insertar_editar_eliminar_con_su_evento_de_calendario()
        {
            var c = _app.Cliente();
            var marca = Marca.Nueva();
            var hoy = Fechas.HoyPeru().AddHours(12);
            await c.PostOkAsync("/MDMenus/Create", "/MDMenus/Create", ("menu_desc", marca), ("menu_precio", "15.50"), ("menu_observ", "con arroz"), ("menu_fecha", Fecha(hoy)));
            var m = await _app.Con(db => db.Menus.AsNoTracking().SingleAsync(x => x.menu_desc == marca));
            Assert.Equal(15.50m, m.menu_precio);
            Assert.True(await _app.Con(db => db.Eventos.AnyAsync(e => e.id_menu == m.id_menu)), "Debe crearse el evento del calendario");

            await c.PostOkAsync($"/MDMenus/Edit/{m.id_menu}", $"/MDMenus/Edit/{m.id_menu}", ("id_menu", m.id_menu), ("menu_desc", marca + "-E"), ("menu_precio", "18"), ("menu_observ", ""), ("menu_fecha", Fecha(hoy)));
            Assert.Equal(18m, await _app.Con(db => db.Menus.Where(x => x.id_menu == m.id_menu).Select(x => x.menu_precio).SingleAsync()));
            Assert.Equal(marca + "-E", await _app.Con(db => db.Eventos.Where(e => e.id_menu == m.id_menu).Select(e => e.title).SingleAsync()));

            await c.PostOkAsync($"/MDMenus/Delete/{m.id_menu}", $"/MDMenus/Delete/{m.id_menu}", ("id", m.id_menu));
            Assert.False(await _app.Con(db => db.Menus.AnyAsync(x => x.id_menu == m.id_menu)));
            Assert.False(await _app.Con(db => db.Eventos.AnyAsync(e => e.id_menu == m.id_menu)));
        }

        [Fact]
        public async Task Tipos_de_pago_insertar_editar_eliminar()
        {
            var c = _app.Cliente();
            var marca = Marca.Nueva();
            await c.PostOkAsync("/MDTipo_pago/Create", "/MDTipo_pago/Create", ("tipago_desc", marca), ("tipago_act", "true"), ("tipago_efectivo", "false"));
            var t = await _app.Con(db => db.Tipo_Pagos.AsNoTracking().SingleAsync(x => x.tipago_desc == marca));
            await c.PostOkAsync($"/MDTipo_pago/Edit/{t.id_tipago}", $"/MDTipo_pago/Edit/{t.id_tipago}", ("id_tipago", t.id_tipago), ("tipago_desc", marca), ("tipago_act", "true"), ("tipago_efectivo", "true"));
            Assert.True(await _app.Con(db => db.Tipo_Pagos.Where(x => x.id_tipago == t.id_tipago).Select(x => x.tipago_efectivo).SingleAsync()));
            await c.PostOkAsync($"/MDTipo_pago/Delete/{t.id_tipago}", $"/MDTipo_pago/Delete/{t.id_tipago}", ("id", t.id_tipago));
            Assert.False(await _app.Con(db => db.Tipo_Pagos.AnyAsync(x => x.id_tipago == t.id_tipago)));
        }

        [Fact]
        public async Task Repartidores_insertar_editar_eliminar()
        {
            var c = _app.Cliente();
            var marca = Marca.Nueva();
            await c.PostOkAsync("/MDDeliveries/Create", "/MDDeliveries/Create", ("deliv_codig", "R1"), ("deliv_nombcom", marca), ("deliv_desc", "Moto"), ("deliv_act", "true"));
            var d = await _app.Con(db => db.Deliveries.AsNoTracking().SingleAsync(x => x.deliv_nombcom == marca));
            await c.PostOkAsync($"/MDDeliveries/Edit/{d.id_deliv}", $"/MDDeliveries/Edit/{d.id_deliv}", ("id_deliv", d.id_deliv), ("deliv_codig", "R1"), ("deliv_nombcom", marca), ("deliv_desc", "Bicicleta"), ("deliv_act", "true"));
            Assert.Equal("Bicicleta", await _app.Con(db => db.Deliveries.Where(x => x.id_deliv == d.id_deliv).Select(x => x.deliv_desc).SingleAsync()));
            await c.PostOkAsync($"/MDDeliveries/Delete/{d.id_deliv}", $"/MDDeliveries/Delete/{d.id_deliv}", ("id", d.id_deliv));
            Assert.False(await _app.Con(db => db.Deliveries.AnyAsync(x => x.id_deliv == d.id_deliv)));
        }

        [Fact]
        public async Task Mesas_insertar_editar_eliminar_conservando_el_plano()
        {
            var c = _app.Cliente();
            var marca = Marca.Nueva();
            var zona = await _app.Con(db => db.Zonas.Where(z => z.zona_act).Select(z => z.id_zona).FirstAsync());
            await c.PostOkAsync("/MDMesas/Create", "/MDMesas/Create", ("mesa_desc", marca), ("mesa_nro", 901), ("mesa_act", "true"), ("mesa_capacidad", 6), ("id_zona", zona));
            var m = await _app.Con(db => db.Mesas.AsNoTracking().SingleAsync(x => x.mesa_desc == marca));
            Assert.Equal(6, m.mesa_capacidad);
            await _app.Con(async db => { var x = await db.Mesas.FindAsync(m.id_mesa); x.mesa_x = 300; x.mesa_y = 200; await db.SaveChangesAsync(); });

            await c.PostOkAsync($"/MDMesas/Edit/{m.id_mesa}", $"/MDMesas/Edit/{m.id_mesa}", ("id_mesa", m.id_mesa), ("mesa_desc", marca + "-E"), ("mesa_nro", 901), ("mesa_act", "true"), ("mesa_capacidad", 8), ("id_zona", zona));
            var editada = await _app.Con(db => db.Mesas.AsNoTracking().SingleAsync(x => x.id_mesa == m.id_mesa));
            Assert.Equal(8, editada.mesa_capacidad);
            Assert.Equal(300, editada.mesa_x);   // la ubicacion en el plano se conserva

            await c.PostOkAsync($"/MDMesas/Delete/{m.id_mesa}", $"/MDMesas/Delete/{m.id_mesa}", ("id", m.id_mesa));
            Assert.False(await _app.Con(db => db.Mesas.AnyAsync(x => x.id_mesa == m.id_mesa)));
        }

        [Fact]
        public async Task Zonas_y_plano_insertar_editar_eliminar()
        {
            var c = _app.Cliente();
            var marca = Marca.Nueva();
            await c.PostOkAsync("/Plano", "/Plano/GuardarZona", ("nombre", marca));
            var zona = await _app.Con(db => db.Zonas.AsNoTracking().SingleAsync(z => z.zona_desc == marca));
            await c.PostOkAsync($"/Plano?zona={zona.id_zona}", "/Plano/GuardarZona", ("id", zona.id_zona), ("nombre", marca + "-E"));
            Assert.Equal(marca + "-E", await _app.Con(db => db.Zonas.Where(z => z.id_zona == zona.id_zona).Select(z => z.zona_desc).SingleAsync()));

            await c.PostOkAsync($"/Plano?zona={zona.id_zona}", "/Plano/NuevaMesa", ("forma", "redonda"), ("zona", zona.id_zona));
            var mesa = await _app.Con(db => db.Mesas.AsNoTracking().SingleAsync(m => m.id_zona == zona.id_zona));
            var r = await c.PostJsonAsync($"/Plano?zona={zona.id_zona}", "/Plano/Guardar", new[] { new { id = mesa.id_mesa, nro = mesa.mesa_nro, desc = marca + " mesa", cap = 5, forma = "redonda", x = 500, y = 300, w = 120, h = 80, rot = 0 } });
            Assert.True(r.IsSuccessStatusCode, "Guardar plano");
            var ubicada = await _app.Con(db => db.Mesas.AsNoTracking().SingleAsync(m => m.id_mesa == mesa.id_mesa));
            Assert.Equal((500, 300, 120, 120, 5), (ubicada.mesa_x.Value, ubicada.mesa_y.Value, ubicada.mesa_ancho, ubicada.mesa_alto, ubicada.mesa_capacidad));

            // Una zona con mesas no se elimina; tras quitar la mesa si
            await c.PostOkAsync($"/Plano?zona={zona.id_zona}", "/Plano/EliminarZona", ("id", zona.id_zona));
            Assert.True(await _app.Con(db => db.Zonas.Where(z => z.id_zona == zona.id_zona).Select(z => z.zona_act).SingleAsync()));
            await c.PostOkAsync($"/Plano?zona={zona.id_zona}", "/Plano/QuitarMesa", ("id", mesa.id_mesa), ("zona", zona.id_zona));
            await c.PostOkAsync($"/Plano?zona={zona.id_zona}", "/Plano/EliminarZona", ("id", zona.id_zona));
            Assert.False(await _app.Con(db => db.Zonas.Where(z => z.id_zona == zona.id_zona).Select(z => z.zona_act).SingleAsync()));

            await _app.Con(async db =>
            {
                db.Mesas.Remove(await db.Mesas.FindAsync(mesa.id_mesa));
                db.Zonas.Remove(await db.Zonas.FindAsync(zona.id_zona));
                await db.SaveChangesAsync();
            });
        }

        [Theory]
        [InlineData("MDEgresos", "egre")]
        [InlineData("MDIngresos", "ingre")]
        [InlineData("MDInicials", "ini")]
        public async Task Movimientos_de_caja_insertar_editar_eliminar(string ctrl, string p)
        {
            var c = _app.Cliente();
            var marca = Marca.Nueva();
            var desc = p == "egre" ? "egre_descr" : $"{p}_desc";
            var monto = p == "egre" ? "egre_costo" : $"{p}_monto";
            var fecha = Fecha(Fechas.AhoraPeru());
            await c.PostOkAsync($"/{ctrl}/Create", $"/{ctrl}/Create", (desc, marca), (monto, "12.50"), ($"{p}_fecha", fecha));

            int id = await _app.Con(db => p switch
            {
                "egre" => db.Egresos.Where(x => x.egre_descr == marca).Select(x => x.id_egre).SingleAsync(),
                "ingre" => db.Ingresos.Where(x => x.ingre_desc == marca).Select(x => x.id_ingre).SingleAsync(),
                _ => db.Inicial.Where(x => x.ini_desc == marca).Select(x => x.id_ini).SingleAsync()
            });
            await c.PostOkAsync($"/{ctrl}/Edit/{id}", $"/{ctrl}/Edit/{id}", ($"id_{p}", id), (desc, marca), (monto, "20"), ($"{p}_fecha", fecha));
            decimal editado = await _app.Con(db => p switch
            {
                "egre" => db.Egresos.Where(x => x.id_egre == id).Select(x => x.egre_costo).SingleAsync(),
                "ingre" => db.Ingresos.Where(x => x.id_ingre == id).Select(x => x.ingre_monto).SingleAsync(),
                _ => db.Inicial.Where(x => x.id_ini == id).Select(x => x.ini_monto).SingleAsync()
            });
            Assert.Equal(20m, editado);

            await c.PostOkAsync($"/{ctrl}/Delete/{id}", $"/{ctrl}/Delete/{id}", ("id", id));
            bool existe = await _app.Con(db => p switch
            {
                "egre" => db.Egresos.AnyAsync(x => x.id_egre == id),
                "ingre" => db.Ingresos.AnyAsync(x => x.id_ingre == id),
                _ => db.Inicial.AnyAsync(x => x.id_ini == id)
            });
            Assert.False(existe);
        }

        [Fact]
        public async Task Configuracion_de_empresa_y_ticketera_se_editan_y_restauran()
        {
            var c = _app.Cliente();
            var original = await _app.Con(db => db.Generales.AsNoTracking().OrderBy(g => g.id_gene).FirstOrDefaultAsync());
            var tk = await _app.Con(db => db.Ticketeras.AsNoTracking().OrderBy(t => t.id_ticketera).FirstOrDefaultAsync());
            try
            {
                await c.PostOkAsync("/Configuracion", "/Configuracion", ("gene_empre", "TEST Empresa"), ("gene_ruc", "20131312955"), ("gene_recordatorio_horas", 3), ("gene_reserva_minutos", 90));
                var g = await _app.Con(db => db.Generales.AsNoTracking().OrderBy(x => x.id_gene).FirstAsync());
                Assert.Equal(("TEST Empresa", 3, 90), (g.gene_empre, g.gene_recordatorio_horas, g.gene_reserva_minutos));

                var r = await c.PostAsync("/Configuracion", "/Configuracion", ("gene_empre", "X"), ("gene_ruc", "123"));
                Assert.Equal(System.Net.HttpStatusCode.OK, r.StatusCode); // RUC invalido: vuelve al formulario

                await c.PostOkAsync("/Ticketera", "/Ticketera", ("tk_modo", 0), ("tk_ancho_mm", 58), ("tk_columnas", 32), ("tk_codepage", 850), ("tk_puerto", 9100), ("tk_corte", 0), ("tk_avance", 2), ("tk_copias", 2), ("tk_pie", "TEST pie"));
                var f = await _app.Con(db => db.Ticketeras.AsNoTracking().OrderBy(x => x.id_ticketera).FirstAsync());
                Assert.Equal((58, 32, 2, "TEST pie"), (f.tk_ancho_mm, f.tk_columnas, f.tk_copias, f.tk_pie));
            }
            finally
            {
                // Se dejan los datos de empresa y ticketera como estaban
                await _app.Con(async db =>
                {
                    var g = await db.Generales.OrderBy(x => x.id_gene).FirstOrDefaultAsync();
                    if (original == null && g != null) db.Generales.Remove(g);
                    else if (g != null) db.Entry(g).CurrentValues.SetValues(original);
                    var t = await db.Ticketeras.OrderBy(x => x.id_ticketera).FirstOrDefaultAsync();
                    if (tk == null && t != null) db.Ticketeras.Remove(t);
                    else if (t != null) db.Entry(t).CurrentValues.SetValues(tk);
                    await db.SaveChangesAsync();
                });
            }
        }
    }
}

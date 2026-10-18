using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planmenus.Migrations
{
    /// <summary>
    /// Catalogos minimos para que el sistema funcione en una base nueva: modulos, submenus,
    /// rol Administrador con todos los permisos y tipos de pago. Los ids de submenu deben
    /// coincidir con Helpers/Permisos.cs. Solo inserta si la tabla esta vacia.
    /// </summary>
    public partial class DatosIniciales : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM dbo.Menuprin)
BEGIN
    SET IDENTITY_INSERT dbo.Menuprin ON;
    INSERT INTO dbo.Menuprin (id_menprin, menuprin_desc, menuprin_act) VALUES
        (1, N'Administración', 1),
        (2, N'Planificación', 1),
        (3, N'Caja', 1);
    SET IDENTITY_INSERT dbo.Menuprin OFF;
END");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM dbo.Submenu)
BEGIN
    SET IDENTITY_INSERT dbo.Submenu ON;
    INSERT INTO dbo.Submenu (id_submenu, id_menprin, submenu_desc, submenu_nro) VALUES
        (1, 1, N'Generales', N'1'),
        (2, 1, N'Usuario', N'2'),
        (3, 1, N'Roles', N'3'),
        (4, 1, N'Menus Modulos', N'4'),
        (5, 1, N'Sub Menu', N'5'),
        (6, 1, N'Rol Usuarios', N'6'),
        (7, 1, N'Roles Permisos', N'7'),
        (8, 2, N'Programacion Menu', N'1'),
        (9, 2, N'Tipo de pago', N'2'),
        (10, 2, N'Reparto', N'3'),
        (11, 2, N'Calendario', N'4'),
        (12, 2, N'Mesas', N'5'),
        (13, 2, N'Egresos', N'6'),
        (14, 2, N'Ingresos', N'7'),
        (15, 2, N'Saldo Inicial', N'8'),
        (18, 3, N'Generar Ticket', N'1'),
        (19, 3, N'Cuadre caja', N'2'),
        (20, 3, N'Lista de Tickets', N'3'),
        (21, 3, N'Reporte resumen por tipo de pago', N'4'),
        (22, 3, N'Reporte ventas tipo de pago', N'5'),
        (23, 3, N'Reporte resumen por plato', N'6'),
        (24, 3, N'Reporte ventas por plato', N'7');
    SET IDENTITY_INSERT dbo.Submenu OFF;
END");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM dbo.Rol)
BEGIN
    SET IDENTITY_INSERT dbo.Rol ON;
    INSERT INTO dbo.Rol (id_rol, rol_desc, rol_act) VALUES (1, N'Administrador', 1);
    SET IDENTITY_INSERT dbo.Rol OFF;
END");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM dbo.Rol_Permisos)
BEGIN
    INSERT INTO dbo.Rol_Permisos (id_rol, id_menprin, id_submenu, rolper_act)
    SELECT 1, s.id_menprin, s.id_submenu, 1 FROM dbo.Submenu s;
END");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM dbo.Tipo_Pago)
BEGIN
    INSERT INTO dbo.Tipo_Pago (tipago_desc, tipago_act) VALUES
        (N'Efectivo', 1), (N'Yape', 1), (N'Plin', 1), (N'Tarjeta', 1);
END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Los catalogos pueden haber sido modificados por los usuarios; no se eliminan.
        }
    }
}

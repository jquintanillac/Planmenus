﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Planmenus.Data;

#nullable disable

namespace Planmenus.Migrations
{
    [DbContext(typeof(Datacontext))]
    [Migration("20220212190019_CajaMigra")]
    partial class CajaMigra
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("Planmenus.Data.Entities.MDCaja", b =>
                {
                    b.Property<int>("id_caja")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("id_caja"), 1L, 1);

                    b.Property<bool>("caja_act")
                        .HasColumnType("bit");

                    b.Property<DateTime>("caja_fecha")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("caja_monto")
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("caja_obs")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("id_deliv")
                        .HasColumnType("int");

                    b.Property<int>("id_menu")
                        .HasColumnType("int");

                    b.Property<int?>("id_mesa")
                        .HasColumnType("int");

                    b.Property<int>("id_tipago")
                        .HasColumnType("int");

                    b.HasKey("id_caja");

                    b.ToTable("Caja");
                });

            modelBuilder.Entity("Planmenus.Data.Entities.MDDelivery", b =>
                {
                    b.Property<int>("id_deliv")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("id_deliv"), 1L, 1);

                    b.Property<bool>("deliv_act")
                        .HasColumnType("bit");

                    b.Property<string>("deliv_codig")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("deliv_desc")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("deliv_nombcom")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("id_deliv");

                    b.ToTable("Delivery");
                });

            modelBuilder.Entity("Planmenus.Data.Entities.MDEgresos", b =>
                {
                    b.Property<int>("id_egre")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("id_egre"), 1L, 1);

                    b.Property<decimal>("egre_costo")
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("egre_descr")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("egre_fecha")
                        .HasColumnType("datetime2");

                    b.HasKey("id_egre");

                    b.ToTable("Egresos");
                });

            modelBuilder.Entity("Planmenus.Data.Entities.MDEventos", b =>
                {
                    b.Property<int>("id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("id"), 1L, 1);

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("EndDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("Location")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("StarDate")
                        .HasColumnType("datetime2");

                    b.Property<int>("id_menu")
                        .HasColumnType("int");

                    b.Property<string>("title")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("id");

                    b.ToTable("Eventos");
                });

            modelBuilder.Entity("Planmenus.Data.Entities.MDGenerales", b =>
                {
                    b.Property<int>("id_gene")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("id_gene"), 1L, 1);

                    b.Property<string>("gene_direc")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("gene_empre")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("gene_ruc")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("gene_telef")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("gene_url")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("id_gene");

                    b.ToTable("Generales");
                });

            modelBuilder.Entity("Planmenus.Data.Entities.MDIngresos", b =>
                {
                    b.Property<int>("id_ingre")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("id_ingre"), 1L, 1);

                    b.Property<string>("ingre_desc")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("ingre_fecha")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("ingre_monto")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("id_ingre");

                    b.ToTable("Ingresos");
                });

            modelBuilder.Entity("Planmenus.Data.Entities.MDInicial", b =>
                {
                    b.Property<int>("id_ini")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("id_ini"), 1L, 1);

                    b.Property<string>("ini_desc")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("ini_fecha")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("ini_monto")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("id_ini");

                    b.ToTable("Inicial");
                });

            modelBuilder.Entity("Planmenus.Data.Entities.MDMenu", b =>
                {
                    b.Property<int>("id_menu")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("id_menu"), 1L, 1);

                    b.Property<string>("menu_desc")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("menu_fecha")
                        .HasColumnType("datetime2");

                    b.Property<string>("menu_observ")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("menu_precio")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("id_menu");

                    b.ToTable("Menu");
                });

            modelBuilder.Entity("Planmenus.Data.Entities.MDMesas", b =>
                {
                    b.Property<int>("id_mesa")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("id_mesa"), 1L, 1);

                    b.Property<bool>("mesa_act")
                        .HasColumnType("bit");

                    b.Property<string>("mesa_desc")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("mesa_nro")
                        .HasColumnType("int");

                    b.HasKey("id_mesa");

                    b.ToTable("Mesas");
                });

            modelBuilder.Entity("Planmenus.Data.Entities.MDTipo_pago", b =>
                {
                    b.Property<int>("id_tipago")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("id_tipago"), 1L, 1);

                    b.Property<bool>("tipago_act")
                        .HasColumnType("bit");

                    b.Property<string>("tipago_desc")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("id_tipago");

                    b.ToTable("Tipo_Pago");
                });

            modelBuilder.Entity("Planmenus.Data.Entities.MDUsuarios", b =>
                {
                    b.Property<int>("IdUsuario")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("IdUsuario"), 1L, 1);

                    b.Property<bool>("Usua_act")
                        .HasColumnType("bit");

                    b.Property<string>("Usua_apellidos")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Usua_email")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Usua_nombres")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Usua_pass")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Usua_user")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("IdUsuario");

                    b.ToTable("Usuarios");
                });
#pragma warning restore 612, 618
        }
    }
}
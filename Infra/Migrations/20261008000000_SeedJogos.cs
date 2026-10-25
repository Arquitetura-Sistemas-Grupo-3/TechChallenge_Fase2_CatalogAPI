using System;
using Infra.Repository;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infra.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261008000000_SeedJogos")]
    public partial class SeedJogos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "INSERT INTO Jogo (Nome, Preco, Data, Ativo) VALUES " +
                "('GTA 6', 1000.00, SYSUTCDATETIME(), 1), " +
                "('Valorant', 149.00, SYSUTCDATETIME(), 1);");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DELETE FROM Jogo WHERE Nome IN ('GTA 6', 'Valorant');");
        }
    }
}

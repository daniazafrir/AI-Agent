using Agent.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Agent.Api.Migrations;

[DbContext(typeof(AgentDbContext))]
[Migration("20260926090000_AddDocumentIndexingDetails")]
public sealed partial class AddDocumentIndexingDetails : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<string>("IndexingDetailsJson", "KnowledgeDocuments", type: "text", nullable: true);
    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn("IndexingDetailsJson", "KnowledgeDocuments");
}

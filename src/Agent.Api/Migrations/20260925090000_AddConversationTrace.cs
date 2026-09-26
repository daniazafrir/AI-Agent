using Agent.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Agent.Api.Migrations;

[DbContext(typeof(AgentDbContext))]
[Migration("20260925090000_AddConversationTrace")]
public sealed partial class AddConversationTrace : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<string>("TraceJson", "ConversationMessages", type: "jsonb", nullable: true);
    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn("TraceJson", "ConversationMessages");
}

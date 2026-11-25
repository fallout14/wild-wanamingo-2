using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    // WW Change - character extras from Misfits: bark voice, body size (height/width), vocal style (speech verb)
    /// <inheritdoc />
    public partial class ProfileCharacterExtras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "bark_voice",
                table: "profile",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "height",
                table: "profile",
                type: "REAL",
                nullable: false,
                defaultValue: 1f);

            migrationBuilder.AddColumn<string>(
                name: "speech_verb",
                table: "profile",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "width",
                table: "profile",
                type: "REAL",
                nullable: false,
                defaultValue: 1f);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "bark_voice", table: "profile");
            migrationBuilder.DropColumn(name: "height", table: "profile");
            migrationBuilder.DropColumn(name: "speech_verb", table: "profile");
            migrationBuilder.DropColumn(name: "width", table: "profile");
        }
    }
}

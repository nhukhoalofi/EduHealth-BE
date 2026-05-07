using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduHealth.Migrations
{
    /// <inheritdoc />
    public partial class AddMessagingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatMessageAttachments",
                columns: table => new
                {
                    AttachmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    FileUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedByUserId = table.Column<int>(type: "int", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessageAttachments", x => x.AttachmentId);
                    table.ForeignKey(
                        name: "FK_ChatMessageAttachments_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    MessageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversationId = table.Column<int>(type: "int", nullable: false),
                    SenderUserId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MessageType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "TEXT"),
                    ClientMessageId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.MessageId);
                    table.ForeignKey(
                        name: "FK_ChatMessages_Users_SenderUserId",
                        column: x => x.SenderUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Conversations",
                columns: table => new
                {
                    ConversationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversationType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "DIRECT"),
                    StudentUserId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    LastMessageId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversations", x => x.ConversationId);
                    table.ForeignKey(
                        name: "FK_Conversations_ChatMessages_LastMessageId",
                        column: x => x.LastMessageId,
                        principalTable: "ChatMessages",
                        principalColumn: "MessageId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Conversations_Students_StudentUserId",
                        column: x => x.StudentUserId,
                        principalTable: "Students",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Conversations_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConversationParticipants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversationId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleInConversation = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastReadMessageId = table.Column<int>(type: "int", nullable: true),
                    LastReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsPinned = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConversationParticipants_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "ConversationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConversationParticipants_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessageAttachments_MessageId",
                table: "ChatMessageAttachments",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessageAttachments_UploadedByUserId",
                table: "ChatMessageAttachments",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ConversationId_MessageId",
                table: "ChatMessages",
                columns: new[] { "ConversationId", "MessageId" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ConversationId_SenderUserId_ClientMessageId",
                table: "ChatMessages",
                columns: new[] { "ConversationId", "SenderUserId", "ClientMessageId" },
                unique: true,
                filter: "[ClientMessageId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ConversationId_SentAt",
                table: "ChatMessages",
                columns: new[] { "ConversationId", "SentAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SenderUserId_SentAt",
                table: "ChatMessages",
                columns: new[] { "SenderUserId", "SentAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_ConversationParticipants_ConversationId_UserId",
                table: "ConversationParticipants",
                columns: new[] { "ConversationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConversationParticipants_UserId_ConversationId",
                table: "ConversationParticipants",
                columns: new[] { "UserId", "ConversationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_CreatedByUserId",
                table: "Conversations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_LastMessageId",
                table: "Conversations",
                column: "LastMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_StudentUserId",
                table: "Conversations",
                column: "StudentUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessageAttachments_ChatMessages_MessageId",
                table: "ChatMessageAttachments",
                column: "MessageId",
                principalTable: "ChatMessages",
                principalColumn: "MessageId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_Conversations_ConversationId",
                table: "ChatMessages",
                column: "ConversationId",
                principalTable: "Conversations",
                principalColumn: "ConversationId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_ChatMessages_LastMessageId",
                table: "Conversations");

            migrationBuilder.DropTable(
                name: "ChatMessageAttachments");

            migrationBuilder.DropTable(
                name: "ConversationParticipants");

            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "Conversations");
        }
    }
}

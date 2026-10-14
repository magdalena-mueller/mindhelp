using Microsoft.EntityFrameworkCore;
using MindHelp.Shared.Models;
//using Supabase.Postgrest.Attributes;

namespace MindHelp.Api;
//alles KI, vorher wars leer
/*public sealed class MindHelpDbContext(DbContextOptions<MindHelpDbContext> options)
    : DbContext(options)
{
    public DbSet<Chat> Chats => Set<Chat>();
    public DbSet<HelpingPersonal> HelpingPersonal => Set<HelpingPersonal>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Chat>(entity =>
        {
            entity.ToTable("chat", "public");
            entity.HasKey(chat => chat.ChatId);

            entity.HasOne(chat => chat.User)
                .WithMany(user => user.Chats)
                .HasForeignKey(chat => chat.UserId)
                .HasPrincipalKey(user => user.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(chat => chat.HelpingPersonal)
                .WithMany(help => help.Chats)
                .HasForeignKey(chat => chat.HelpingPersonalId)
                .HasPrincipalKey(help => help.HelpingPersonalId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<HelpingPersonal>(entity =>
        {
            entity.ToTable("psychologischesPersonal", "public"); 
            entity.HasKey(help => help.HelpingPersonalId);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users", "public");
            entity.HasKey(user => user.UserId);
        });
    }
}*/
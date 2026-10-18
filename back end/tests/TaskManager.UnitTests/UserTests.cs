using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Exceptions;
using static TaskManager.Domain.Enums.Enums;
using static TaskManager.Domain.Exceptions.DomainExceptions;

namespace TaskManager.UnitTests;

public class UserTests
{
    private static readonly DateTime Now = new(2025, 6, 10, 12, 0, 0, DateTimeKind.Utc);

    private static User NewUser(string email = "Ali@Example.com") =>
        User.Create("Ali", email, "hash", UserRole.User, Now);

    [Fact]
    public void Emails_differing_only_by_case_normalize_to_the_same_value()
        => Assert.Equal(NewUser("Ali@Example.com").NormalizedEmail, NewUser("  ali@example.COM ").NormalizedEmail);

    [Fact]
    public void Admin_cannot_delete_own_account()
    {
        var admin = User.Create("Admin", "a@a.com", "hash", UserRole.Admin, Now);
        Assert.Throws<DomainValidationException>(() => admin.SoftDelete(admin.Id, Now));
        Assert.False(admin.IsDeleted);
    }

    [Fact]
    public void Deleting_another_user_marks_them_deleted()
    {
        var user = NewUser();
        user.SoftDelete(Guid.NewGuid(), Now);
        Assert.True(user.IsDeleted);
        Assert.Equal(Now, user.DeletedAt);
    }
}
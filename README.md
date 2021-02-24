# Paylocity Code Challenge

This is my submission for the Paylocity Code Challenge.

### Toolkit

* Visual Studio 2019
* ASP.Net Core 5
  * Migrations
* Entity Framework 6
* Web API
* ASP.Net Core Identity
* JSON Web Tokens
* Bootstrap 4.6

### Core requirements

The challenge's core requirements have been implemented. Once logged into a user with the necessary permissions 
(see notes, below), navigate to add or view users and to see the benefits and pay estimates.

### Notes

* Known to work in the current versions of Firefox, Chrome, and Edge.

* Authentication has been implemented and some users have been seeded:

| Username               | Password | Roles |
|------------------------|----------|-------|
| superadmin@example.com | password | `ViewSelf`, `ViewOther`, `AddEdit`, `Remove`, `PermissionsView`, `PermissionsEdit` |
| seeduser01@example.com | password | ( No roles ) |
| seeduser02@example.com | password | `ViewSelf` |
| seeduser03@example.com | password | `ViewSelf`, `ViewOther` |
| seeduser04@example.com | password | `ViewSelf`, `ViewOther`, `AddEdit` |
| seeduser05@example.com | password | `ViewSelf` |

* `Remove` has not yet been implemented. Users cannot currently be deleted.
* `PermissionsView` and `PermissionsEdit` have not yet been implemented. Roles cannot currently be viewed or edited.
* Passwords for new users require all of:
  * At least one uppercase letter
  * At least one lowercase letter
  * At least one digit
  * At least one of `#`, `$`, `^`, `+`, `=`, `!`, `*`, `(`, `)`, `@`, `%`, `&`
  * At least 8 characters
* New users can log in but, since they won't have permissions, they can't do anything.

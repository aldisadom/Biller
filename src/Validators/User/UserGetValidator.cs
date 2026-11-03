using BillerContracts.Requests.User;

namespace Validators.User;

/// <summary>
/// User list query validation: paging only
/// </summary>
public class UserGetValidator : PagedRequestValidator<UserGetRequest>;

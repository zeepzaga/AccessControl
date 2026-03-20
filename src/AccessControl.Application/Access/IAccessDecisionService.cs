using System.Threading;
using System.Threading.Tasks;

namespace AccessControl.Application.Access;

public interface IAccessDecisionService
{
    Task<AccessDecision> ProcessCardReadAsync(CardReadRequest request, CancellationToken cancellationToken = default);
}

namespace Mockolate.Frameworks.TestHelpers.Domain;

public class TransferService(IAccount source, IAccount target)
{
	public void Transfer(decimal amount)
	{
		source.Withdraw(amount);
		target.Deposit(amount);
	}
}

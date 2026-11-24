namespace Mockolate.TUnit.Tests.Domain;

public class TransferService(IAccount source, IAccount target)
{
	public void Transfer(decimal amount)
	{
		source.Withdraw(amount);
		target.Deposit(amount);
	}
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using Mockolate.Interactions;

namespace Mockolate.Setup;

internal partial class MockSetups
{
	[DebuggerDisplay("{ToString()}")]
#if !DEBUG
	[DebuggerNonUserCode]
#endif
	internal sealed class PropertySetups
	{
		private int _count;
		private PropertySetup[]? _storage;
		private readonly MockolateLock _writeLock = new();

		public int Count => Volatile.Read(ref _count);

		public void Add(PropertySetup setup)
		{
			lock (_writeLock)
			{
				PropertySetup[] old = _storage ?? [];
				int existingIndex = -1;
				string setupName = setup.Name;
				for (int i = 0; i < old.Length; i++)
				{
					if (string.Equals(old[i].Name, setupName, StringComparison.Ordinal))
					{
						existingIndex = i;
						break;
					}
				}

				if (existingIndex >= 0)
				{
					bool wasDefault = old[existingIndex] is PropertySetup.Default;
					bool isDefault = setup is PropertySetup.Default;
					// Never overwrite a user-configured setup with a default placeholder:
					// a first property access can race with Setup...InitializeWith(...) and
					// otherwise lose the user's setup.
					if (isDefault && !wasDefault)
					{
						return;
					}

					PropertySetup[] next = new PropertySetup[old.Length];
					Array.Copy(old, next, old.Length);
					next[existingIndex] = setup;
					Volatile.Write(ref _storage, next);
					if (wasDefault && !isDefault)
					{
						Volatile.Write(ref _count, _count + 1);
					}
				}
				else
				{
					PropertySetup[] next = new PropertySetup[old.Length + 1];
					Array.Copy(old, next, old.Length);
					next[old.Length] = setup;
					Volatile.Write(ref _storage, next);
					if (setup is not PropertySetup.Default)
					{
						Volatile.Write(ref _count, _count + 1);
					}
				}
			}
		}

		public bool TryGetValue(string propertyName, [NotNullWhen(true)] out PropertySetup? setup)
		{
			PropertySetup[]? storage = Volatile.Read(ref _storage);
			if (storage is not null)
			{
				for (int i = 0; i < storage.Length; i++)
				{
					if (string.Equals(storage[i].Name, propertyName, StringComparison.Ordinal))
					{
						setup = storage[i];
						return true;
					}
				}
			}

			setup = null;
			return false;
		}

		internal IEnumerable<PropertySetup> EnumerateUnusedSetupsBy(IMockInteractions interactions)
		{
			PropertySetup[]? storage = Volatile.Read(ref _storage);
			if (storage is null || storage.Length == 0)
			{
				return [];
			}

			return storage.Where(propertySetup => propertySetup is not PropertySetup.Default &&
			                                      interactions.OfType<PropertyAccess>()
				                                      .All(propertyAccess => !((IInteractivePropertySetup)propertySetup).Matches(propertyAccess)))
				.ToList();
		}

		/// <inheritdoc cref="object.ToString()" />
		[ExcludeFromCodeCoverage]
		public override string ToString()
		{
			PropertySetup[]? storage = Volatile.Read(ref _storage);
			if (storage is null || storage.Length == 0)
			{
				return "0 properties";
			}

			List<PropertySetup>? setups = null;
			foreach (PropertySetup setup in storage)
			{
				if (setup is not PropertySetup.Default)
				{
					setups ??= [];
					setups.Add(setup);
				}
			}

			if (setups is null || setups.Count == 0)
			{
				return "0 properties";
			}

			StringBuilder sb = new();
			sb.Append(setups.Count).Append(setups.Count == 1 ? " property:" : " properties:").AppendLine();
			foreach (PropertySetup item in setups)
			{
				sb.Append(item).Append(' ').Append(item.Name).AppendLine();
			}

			sb.Length -= Environment.NewLine.Length;
			return sb.ToString();
		}
	}
}

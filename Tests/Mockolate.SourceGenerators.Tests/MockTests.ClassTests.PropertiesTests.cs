namespace Mockolate.SourceGenerators.Tests;

public sealed partial class MockTests
{
	public sealed partial class ClassTests
	{
		public sealed class PropertiesTests
		{
			[Test]
			[Arguments("string Name { get; }", "Setup.IPropertyGetterOnlySetup<string>",
				"Verify.VerificationPropertyGetterResult<IMockVerifyForIMyService>",
				"Setup.PropertySetup<string>", "Verify.VerificationPropertyResult<IMockVerifyForIMyService, string>")]
			[Arguments("string Name { set; }", "Setup.IPropertySetterOnlySetup<string>",
				"Verify.VerificationPropertySetterResult<IMockVerifyForIMyService, string>",
				"Setup.PropertySetup<string>", "Verify.VerificationPropertyResult<IMockVerifyForIMyService, string>")]
			[Arguments("string Name { get; set; }", "Setup.PropertySetup<string>",
				"Verify.VerificationPropertyResult<IMockVerifyForIMyService, string>",
				"Setup.IPropertyGetterOnlySetup<string>", "Verify.VerificationPropertyGetterResult<IMockVerifyForIMyService>")]
			public async Task PropertySurface_ShouldOnlyExposeTheAccessorsTheMockIntercepts(
				string property, string expectedSetupType, string expectedVerifyType,
				string unexpectedSetupType, string unexpectedVerifyType)
			{
				GeneratorResult result = Generator
					.Run($$"""
					       using Mockolate;

					       namespace MyCode;
					       public class Program
					       {
					           public static void Main(string[] args)
					           {
					       		_ = IMyService.CreateMock();
					           }
					       }

					       public interface IMyService
					       {
					           {{property}}
					       }
					       """);

				await That(result.Diagnostics).IsEmpty();
				await That(result.Sources["Mock.IMyService.g.cs"])
					.Contains($"global::Mockolate.{expectedSetupType} Name {{ get; }}").And
					.Contains($"global::Mockolate.{expectedVerifyType} Name {{ get; }}").And
					.DoesNotContain($"global::Mockolate.{unexpectedSetupType} Name {{ get; }}").And
					.DoesNotContain($"global::Mockolate.{unexpectedVerifyType} Name {{ get; }}")
					.Because("a widened facade would silently re-expose an accessor the mock never intercepts");
			}

			[Test]
			public async Task InitOnlyProperty_ShouldEmitInitAccessorAndCompile()
			{
				GeneratorResult result = Generator
					.Run("""
					     using Mockolate;

					     namespace MyCode;
					     public class Program
					     {
					         public static void Main(string[] args)
					         {
					     		_ = IMyService.CreateMock();
					         }
					     }

					     public interface IMyService
					     {
					         string Name { get; init; }
					     }
					     """);

				await That(result.Diagnostics).IsEmpty();
				await That(result.Sources["Mock.IMyService.g.cs"])
					.Contains("""
					          		public string Name
					          		{
					          			get
					          			{
					          				return this.MockRegistry.GetPropertyFast<string>(global::Mockolate.Mock.IMyService.MemberId_Name_Get, global::Mockolate.Mock.IMyService.PropertyAccess_Name_Get, static b => b.DefaultValue.Generate(default(string)!), this.MockRegistry.Wraps is not global::MyCode.IMyService wraps ? null : () => wraps.Name);
					          			}
					          			init
					          			{
					          				this.MockRegistry.SetPropertyFast<string>(global::Mockolate.Mock.IMyService.MemberId_Name_Get, global::Mockolate.Mock.IMyService.MemberId_Name_Set, "global::MyCode.IMyService.Name", value);
					          			}
					          		}
					          """).IgnoringNewlineStyle();
			}

			[Test]
			public async Task MultipleImplementations_ShouldOnlyHaveOneExplicitImplementation()
			{
				GeneratorResult result = Generator
					.Run("""
					     using System;
					     using Mockolate;

					     namespace MyCode;
					     public class Program
					     {
					         public static void Main(string[] args)
					         {
					     		_ = IMyService.CreateMock().Implementing<IMyServiceBase1>().Implementing<IMyServiceBase2>();
					         }
					     }

					     public interface IMyService : IMyServiceBase1
					     {
					         new string Value { get; set; }
					     }

					     public interface IMyServiceBase1 : IMyServiceBase2
					     {
					         new int Value { get; set; }
					     }

					     public interface IMyServiceBase2
					     {
					         long Value { get; set; }
					     }
					     """);

				await That(result.Sources).ContainsKey("Mock.IMyService__IMyServiceBase1__IMyServiceBase2.g.cs");
				await That(result.Sources["Mock.IMyService__IMyServiceBase1__IMyServiceBase2.g.cs"])
					.Contains("public string Value").Once().And
					.Contains("int global::MyCode.IMyServiceBase1.Value").Once().And
					.Contains("long global::MyCode.IMyServiceBase2.Value").Once();
			}

			[Test]
			public async Task
				MultipleStaticAbstractProperties_WithCollidingName_ShouldEmitStaticKeywordOnExplicitImplementation()
			{
				GeneratorResult result = Generator
					.Run("""
					     using Mockolate;

					     namespace MyCode;
					     public class Program
					     {
					         public static void Main(string[] args)
					         {
					     		_ = IDerived.CreateMock();
					         }
					     }

					     public interface IBase
					     {
					         static abstract int Counter { get; }
					     }

					     public interface IDerived : IBase
					     {
					         new static abstract int Counter { get; }
					     }
					     """);

				await That(result.Sources).ContainsKey("Mock.IDerived.g.cs");
				await That(result.Sources["Mock.IDerived.g.cs"])
					.Contains("static int global::MyCode.IBase.Counter")
					.Because(
						"a colliding static abstract property on a base interface must be emitted as an explicit static implementation, with the static keyword preserved");
			}

			[Test]
			public async Task RefReturn_ShouldCompile()
			{
				GeneratorResult result = Generator
					.Run("""
					     using Mockolate;

					     namespace MyCode;
					     public class Program
					     {
					         public static void Main(string[] args)
					         {
					     		_ = IMyService.CreateMock();
					         }
					     }

					     public interface IMyService
					     {
					         ref int GetByRef();
					         ref readonly int GetByRefReadonly();
					     }
					     """);

				await That(result.Diagnostics).IsEmpty();
			}

			[Test]
			public async Task ShouldImplementAllPropertiesFromInterfaces()
			{
				GeneratorResult result = Generator
					.Run("""
					     using System;
					     using Mockolate;

					     namespace MyCode;
					     public class Program
					     {
					         public static void Main(string[] args)
					         {
					     		_ = IMyService.CreateMock();
					         }
					     }

					     public interface IMyService
					     {
					         int SomeProperty { get; set; }
					         bool? SomeReadOnlyProperty { get; }
					         bool? SomeWriteOnlyProperty { set; }
					         internal int SomeInternalProperty { get; set; }
					         private int SomePrivateProperty { get; set; }
					         private protected int SomePrivateProtectedProperty { get; set; }
					     }
					     """);

				await That(result.Sources).ContainsKey("Mock.IMyService.g.cs");
				await That(result.Sources["Mock.IMyService.g.cs"])
					.Contains("""
					          		/// <inheritdoc cref="global::MyCode.IMyService.SomeProperty" />
					          		public int SomeProperty
					          		{
					          			get
					          			{
					          				return this.MockRegistry.GetPropertyFast<int>(global::Mockolate.Mock.IMyService.MemberId_SomeProperty_Get, global::Mockolate.Mock.IMyService.PropertyAccess_SomeProperty_Get, static b => b.DefaultValue.Generate(default(int)!), this.MockRegistry.Wraps is not global::MyCode.IMyService wraps ? null : () => wraps.SomeProperty);
					          			}
					          			set
					          			{
					          				this.MockRegistry.SetPropertyFast<int>(global::Mockolate.Mock.IMyService.MemberId_SomeProperty_Get, global::Mockolate.Mock.IMyService.MemberId_SomeProperty_Set, "global::MyCode.IMyService.SomeProperty", value);
					          				if (this.MockRegistry.Wraps is global::MyCode.IMyService wraps)
					          				{
					          					wraps.SomeProperty = value;
					          				}
					          			}
					          		}
					          """).IgnoringNewlineStyle().And
					.Contains("""
					          		/// <inheritdoc cref="global::MyCode.IMyService.SomeReadOnlyProperty" />
					          		public bool? SomeReadOnlyProperty
					          		{
					          			get
					          			{
					          				return this.MockRegistry.GetPropertyFast<bool?>(global::Mockolate.Mock.IMyService.MemberId_SomeReadOnlyProperty_Get, global::Mockolate.Mock.IMyService.PropertyAccess_SomeReadOnlyProperty_Get, static b => b.DefaultValue.Generate(default(bool?)!), this.MockRegistry.Wraps is not global::MyCode.IMyService wraps ? null : () => wraps.SomeReadOnlyProperty);
					          			}
					          		}
					          """).IgnoringNewlineStyle().And
					.Contains("""
					          		/// <inheritdoc cref="global::MyCode.IMyService.SomeWriteOnlyProperty" />
					          		public bool? SomeWriteOnlyProperty
					          		{
					          			set
					          			{
					          				this.MockRegistry.SetPropertyFast<bool?>(global::Mockolate.Mock.IMyService.MemberId_SomeWriteOnlyProperty_Get, global::Mockolate.Mock.IMyService.MemberId_SomeWriteOnlyProperty_Set, "global::MyCode.IMyService.SomeWriteOnlyProperty", value);
					          				if (this.MockRegistry.Wraps is global::MyCode.IMyService wraps)
					          				{
					          					wraps.SomeWriteOnlyProperty = value;
					          				}
					          			}
					          		}
					          """).IgnoringNewlineStyle().And
					.Contains("""
					          		/// <inheritdoc cref="global::MyCode.IMyService.SomeInternalProperty" />
					          		internal int SomeInternalProperty
					          		{
					          			get
					          			{
					          				return this.MockRegistry.GetPropertyFast<int>(global::Mockolate.Mock.IMyService.MemberId_SomeInternalProperty_Get, global::Mockolate.Mock.IMyService.PropertyAccess_SomeInternalProperty_Get, static b => b.DefaultValue.Generate(default(int)!), this.MockRegistry.Wraps is not global::MyCode.IMyService wraps ? null : () => wraps.SomeInternalProperty);
					          			}
					          			set
					          			{
					          				this.MockRegistry.SetPropertyFast<int>(global::Mockolate.Mock.IMyService.MemberId_SomeInternalProperty_Get, global::Mockolate.Mock.IMyService.MemberId_SomeInternalProperty_Set, "global::MyCode.IMyService.SomeInternalProperty", value);
					          				if (this.MockRegistry.Wraps is global::MyCode.IMyService wraps)
					          				{
					          					wraps.SomeInternalProperty = value;
					          				}
					          			}
					          		}
					          """).IgnoringNewlineStyle().And
					.Contains("""
					          		/// <inheritdoc cref="global::MyCode.IMyService.SomePrivateProperty" />
					          		private int SomePrivateProperty
					          		{
					          			get
					          			{
					          				return this.MockRegistry.GetPropertyFast<int>(global::Mockolate.Mock.IMyService.MemberId_SomePrivateProperty_Get, global::Mockolate.Mock.IMyService.PropertyAccess_SomePrivateProperty_Get, static b => b.DefaultValue.Generate(default(int)!), this.MockRegistry.Wraps is not global::MyCode.IMyService wraps ? null : () => wraps.SomePrivateProperty);
					          			}
					          			set
					          			{
					          				this.MockRegistry.SetPropertyFast<int>(global::Mockolate.Mock.IMyService.MemberId_SomePrivateProperty_Get, global::Mockolate.Mock.IMyService.MemberId_SomePrivateProperty_Set, "global::MyCode.IMyService.SomePrivateProperty", value);
					          				if (this.MockRegistry.Wraps is global::MyCode.IMyService wraps)
					          				{
					          					wraps.SomePrivateProperty = value;
					          				}
					          			}
					          		}
					          """).IgnoringNewlineStyle().And
					.Contains("""
					          		/// <inheritdoc cref="global::MyCode.IMyService.SomePrivateProtectedProperty" />
					          		private protected int SomePrivateProtectedProperty
					          		{
					          			get
					          			{
					          				return this.MockRegistry.GetPropertyFast<int>(global::Mockolate.Mock.IMyService.MemberId_SomePrivateProtectedProperty_Get, global::Mockolate.Mock.IMyService.PropertyAccess_SomePrivateProtectedProperty_Get, static b => b.DefaultValue.Generate(default(int)!), this.MockRegistry.Wraps is not global::MyCode.IMyService wraps ? null : () => wraps.SomePrivateProtectedProperty);
					          			}
					          			set
					          			{
					          				this.MockRegistry.SetPropertyFast<int>(global::Mockolate.Mock.IMyService.MemberId_SomePrivateProtectedProperty_Get, global::Mockolate.Mock.IMyService.MemberId_SomePrivateProtectedProperty_Set, "global::MyCode.IMyService.SomePrivateProtectedProperty", value);
					          				if (this.MockRegistry.Wraps is global::MyCode.IMyService wraps)
					          				{
					          					wraps.SomePrivateProtectedProperty = value;
					          				}
					          			}
					          		}
					          """).IgnoringNewlineStyle();
			}

			[Test]
			public async Task ShouldImplementImplicitlyInheritedProperties()
			{
				GeneratorResult result = Generator
					.Run("""
					     using System;
					     using Mockolate;

					     namespace MyCode;
					     public class Program
					     {
					         public static void Main(string[] args)
					         {
					     		_ = IMyService.CreateMock();
					         }
					     }

					     public interface IMyService : IMyServiceBase1
					     {
					         int MyDirectProperty { get; set; }
					     }

					     public interface IMyServiceBase1 : IMyServiceBase2
					     {
					         int MyBaseProperty1 { get; set; }
					     }

					     public interface IMyServiceBase2 : IMyServiceBase3
					     {
					         int MyBaseProperty2 { get; set; }
					     }

					     public interface IMyServiceBase3
					     {
					         int MyBaseProperty3 { get; set; }
					     }
					     """);

				await That(result.Sources).ContainsKey("Mock.IMyService.g.cs");
				await That(result.Sources["Mock.IMyService.g.cs"])
					.Contains("""
					          		/// <inheritdoc cref="global::MyCode.IMyService.MyDirectProperty" />
					          		public int MyDirectProperty
					          		{
					          			get
					          			{
					          				return this.MockRegistry.GetPropertyFast<int>(global::Mockolate.Mock.IMyService.MemberId_MyDirectProperty_Get, global::Mockolate.Mock.IMyService.PropertyAccess_MyDirectProperty_Get, static b => b.DefaultValue.Generate(default(int)!), this.MockRegistry.Wraps is not global::MyCode.IMyService wraps ? null : () => wraps.MyDirectProperty);
					          			}
					          			set
					          			{
					          				this.MockRegistry.SetPropertyFast<int>(global::Mockolate.Mock.IMyService.MemberId_MyDirectProperty_Get, global::Mockolate.Mock.IMyService.MemberId_MyDirectProperty_Set, "global::MyCode.IMyService.MyDirectProperty", value);
					          				if (this.MockRegistry.Wraps is global::MyCode.IMyService wraps)
					          				{
					          					wraps.MyDirectProperty = value;
					          				}
					          			}
					          		}
					          """).IgnoringNewlineStyle().And
					.Contains("""
					          		/// <inheritdoc cref="global::MyCode.IMyServiceBase1.MyBaseProperty1" />
					          		public int MyBaseProperty1
					          		{
					          			get
					          			{
					          				return this.MockRegistry.GetPropertyFast<int>(global::Mockolate.Mock.IMyService.MemberId_MyBaseProperty1_Get, global::Mockolate.Mock.IMyService.PropertyAccess_MyBaseProperty1_Get, static b => b.DefaultValue.Generate(default(int)!), this.MockRegistry.Wraps is not global::MyCode.IMyServiceBase1 wraps ? null : () => wraps.MyBaseProperty1);
					          			}
					          			set
					          			{
					          				this.MockRegistry.SetPropertyFast<int>(global::Mockolate.Mock.IMyService.MemberId_MyBaseProperty1_Get, global::Mockolate.Mock.IMyService.MemberId_MyBaseProperty1_Set, "global::MyCode.IMyServiceBase1.MyBaseProperty1", value);
					          				if (this.MockRegistry.Wraps is global::MyCode.IMyServiceBase1 wraps)
					          				{
					          					wraps.MyBaseProperty1 = value;
					          				}
					          			}
					          		}
					          """).IgnoringNewlineStyle().And
					.Contains("""
					          		/// <inheritdoc cref="global::MyCode.IMyServiceBase2.MyBaseProperty2" />
					          		public int MyBaseProperty2
					          		{
					          			get
					          			{
					          				return this.MockRegistry.GetPropertyFast<int>(global::Mockolate.Mock.IMyService.MemberId_MyBaseProperty2_Get, global::Mockolate.Mock.IMyService.PropertyAccess_MyBaseProperty2_Get, static b => b.DefaultValue.Generate(default(int)!), this.MockRegistry.Wraps is not global::MyCode.IMyServiceBase2 wraps ? null : () => wraps.MyBaseProperty2);
					          			}
					          			set
					          			{
					          				this.MockRegistry.SetPropertyFast<int>(global::Mockolate.Mock.IMyService.MemberId_MyBaseProperty2_Get, global::Mockolate.Mock.IMyService.MemberId_MyBaseProperty2_Set, "global::MyCode.IMyServiceBase2.MyBaseProperty2", value);
					          				if (this.MockRegistry.Wraps is global::MyCode.IMyServiceBase2 wraps)
					          				{
					          					wraps.MyBaseProperty2 = value;
					          				}
					          			}
					          		}
					          """).IgnoringNewlineStyle().And
					.Contains("""
					          		/// <inheritdoc cref="global::MyCode.IMyServiceBase3.MyBaseProperty3" />
					          		public int MyBaseProperty3
					          		{
					          			get
					          			{
					          				return this.MockRegistry.GetPropertyFast<int>(global::Mockolate.Mock.IMyService.MemberId_MyBaseProperty3_Get, global::Mockolate.Mock.IMyService.PropertyAccess_MyBaseProperty3_Get, static b => b.DefaultValue.Generate(default(int)!), this.MockRegistry.Wraps is not global::MyCode.IMyServiceBase3 wraps ? null : () => wraps.MyBaseProperty3);
					          			}
					          			set
					          			{
					          				this.MockRegistry.SetPropertyFast<int>(global::Mockolate.Mock.IMyService.MemberId_MyBaseProperty3_Get, global::Mockolate.Mock.IMyService.MemberId_MyBaseProperty3_Set, "global::MyCode.IMyServiceBase3.MyBaseProperty3", value);
					          				if (this.MockRegistry.Wraps is global::MyCode.IMyServiceBase3 wraps)
					          				{
					          					wraps.MyBaseProperty3 = value;
					          				}
					          			}
					          		}
					          """).IgnoringNewlineStyle();
			}

			[Test]
			public async Task
				ShouldImplementVirtualPropertiesOfClassesAndAllExplicitlyFromAdditionalInterfaces()
			{
				GeneratorResult result = Generator
					.Run("""
					     using System;
					     using Mockolate;

					     namespace MyCode;
					     public class Program
					     {
					         public static void Main(string[] args)
					         {
					     		_ = MyService.CreateMock().Implementing<IMyOtherService>();
					     		_ = MyProtectedService.CreateMock();
					         }
					     }

					     public class MyService
					     {
					         public virtual int SomeProperty1 { protected get; set; }
					         public virtual int SomeProperty2 { get; protected set; }
					         protected virtual bool? SomeReadOnlyProperty { get; }
					         protected virtual bool? SomeWriteOnlyProperty { set; }
					         public bool? SomeNonVirtualProperty { get; set; }
					     }

					     public class MyProtectedService
					     {
					         protected virtual bool? SomeProperty { get; set; }
					     }

					     public interface IMyOtherService
					     {
					         int SomeAdditionalProperty { get; set; }
					     }
					     """);

				await That(result.Sources).ContainsKey("Mock.MyService__IMyOtherService.g.cs");
				await That(result.Sources["Mock.MyService__IMyOtherService.g.cs"])
					.Contains("""
					          		/// <inheritdoc cref="global::MyCode.MyService.SomeProperty1" />
					          		public override int SomeProperty1
					          		{
					          			protected get
					          			{
					          				return this.MockRegistry.GetProperty<int>(global::Mockolate.Mock.MyService__IMyOtherService.PropertyAccess_SomeProperty1_Get, () => this.MockRegistry.Behavior.DefaultValue.Generate(default(int)!), () => base.SomeProperty1);
					          			}
					          			set
					          			{
					          				if (!this.MockRegistry.SetProperty<int>("global::MyCode.MyService.SomeProperty1", value))
					          				{
					          					if (this.MockRegistry.Wraps is global::MyCode.MyService wraps)
					          					{
					          						wraps.SomeProperty1 = value;
					          					}
					          					else
					          					{
					          						base.SomeProperty1 = value;
					          					}
					          				}
					          			}
					          		}
					          """).IgnoringNewlineStyle().And
					.Contains("""
					          		/// <inheritdoc cref="global::MyCode.MyService.SomeProperty2" />
					          		public override int SomeProperty2
					          		{
					          			get
					          			{
					          				return this.MockRegistry.GetProperty<int>(global::Mockolate.Mock.MyService__IMyOtherService.PropertyAccess_SomeProperty2_Get, () => this.MockRegistry.Behavior.DefaultValue.Generate(default(int)!), this.MockRegistry.Wraps is global::MyCode.MyService wraps ? () => wraps.SomeProperty2 : () => base.SomeProperty2);
					          			}
					          			protected set
					          			{
					          				if (!this.MockRegistry.SetProperty<int>("global::MyCode.MyService.SomeProperty2", value))
					          				{
					          					base.SomeProperty2 = value;
					          				}
					          			}
					          		}
					          """).IgnoringNewlineStyle().And
					.Contains("""
					          		/// <inheritdoc cref="global::MyCode.MyService.SomeReadOnlyProperty" />
					          		protected override bool? SomeReadOnlyProperty
					          		{
					          			get
					          			{
					          				return this.MockRegistry.GetProperty<bool?>(global::Mockolate.Mock.MyService__IMyOtherService.PropertyAccess_SomeReadOnlyProperty_Get, () => this.MockRegistry.Behavior.DefaultValue.Generate(default(bool?)!), () => base.SomeReadOnlyProperty);
					          			}
					          		}
					          """).IgnoringNewlineStyle().And
					.Contains("""
					          		/// <inheritdoc cref="global::MyCode.MyService.SomeWriteOnlyProperty" />
					          		protected override bool? SomeWriteOnlyProperty
					          		{
					          			set
					          			{
					          				if (!this.MockRegistry.SetProperty<bool?>("global::MyCode.MyService.SomeWriteOnlyProperty", value))
					          				{
					          					base.SomeWriteOnlyProperty = value;
					          				}
					          			}
					          		}
					          """).IgnoringNewlineStyle().And
					.DoesNotContain("SomeNonVirtualProperty").Because("The property is not virtual!").And
					.Contains("""
					          		/// <inheritdoc cref="global::MyCode.IMyOtherService.SomeAdditionalProperty" />
					          		int global::MyCode.IMyOtherService.SomeAdditionalProperty
					          		{
					          			get
					          			{
					          				return this.MockRegistry.GetProperty<int>(global::Mockolate.Mock.MyService__IMyOtherService.PropertyAccess_SomeAdditionalProperty_Get, () => this.MockRegistry.Behavior.DefaultValue.Generate(default(int)!), null);
					          			}
					          			set
					          			{
					          				this.MockRegistry.SetProperty<int>("global::MyCode.IMyOtherService.SomeAdditionalProperty", value);
					          			}
					          		}
					          """).IgnoringNewlineStyle();
			}
		}
	}
}

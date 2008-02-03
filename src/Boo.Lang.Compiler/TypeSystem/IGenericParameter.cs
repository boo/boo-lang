namespace Boo.Lang.Compiler.TypeSystem
{
	public interface IGenericParameter: IType
	{
		IType DeclaringType { get; }
		IMethod DeclaringMethod { get; } 
		int GenericParameterPosition { get; }

		bool MustHaveDefaultConstructor { get; }
		IType[] GetTypeConstraints();
		Variance Variance { get; }
	}
}

"""
[Boo.Lang.ModuleAttribute]
public final transient class Equality0Module(System.Object):

	private static def Main(argv as (System.String)) as System.Void:
		o1 = object()
		o2 = object()
		Boo.Lang.Builtins.print(System.String.op_Equality('foo', 'bar'))
		Boo.Lang.Builtins.print((3 == 3.0))
		Boo.Lang.Builtins.print(Boo.Lang.Runtime.RuntimeServices.EqualityOperator(o1, o2))
		Boo.Lang.Builtins.print(Boo.Lang.Runtime.RuntimeServices.EqualityOperator('foo', o2))
		Boo.Lang.Builtins.print(Boo.Lang.Runtime.RuntimeServices.EqualityOperator(3.0, o1))

	private def constructor():
		super()
"""
o1 = object()
o2 = object()
print('foo' == 'bar')
print(3 == 3.0)
print(o1 == o2)
print('foo' == o2)
print(3.0 == o1)

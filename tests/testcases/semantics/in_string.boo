"""
[Boo.Lang.ModuleAttribute]
public final transient class In_stringModule(System.Object):

	private static def Main(argv as (System.String)) as System.Void:
		Boo.Lang.Builtins.print(Boo.Lang.RuntimeServices.op_Member('f', 'foo'))
		Boo.Lang.Builtins.print(Boo.Lang.RuntimeServices.op_NotMember('f', 'foo'))

	private def constructor():
		super()
"""
print("f" in "foo")
print("f" not in "foo")

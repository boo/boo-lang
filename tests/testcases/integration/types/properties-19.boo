interface IFoo:
	Bar:
		get
		set
		
class Foo(IFoo):
	[property(Bar)]
	_bar

f as IFoo = Foo()
assert f.Bar is null
f.Bar = "value"
assert "value" == f.Bar

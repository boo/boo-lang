"""
BCE0154.boo(5,13): BCE0154: Generic parameter 'T' cannot have both a value type constraint and a default constructor constraint.
"""

class C2[of T(struct, constructor)]:
	pass

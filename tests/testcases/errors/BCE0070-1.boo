"""
BCE0070-1.boo(4,5): BCE0070: Recursive and mutually recursive methods must declare their return types.
"""
def fatorial(value as int):
	return 1 if value < 2
	return value*fatorial(value-1)
	

"""
evaluated
if
evaluated
inside
"""
def fun():
	print('evaluated')
	return true
	
a = fun() or fun()
print('if')
if fun() or fun():
	print('inside')

"""
1
2
3
4
5
6
"""

import System.Collections.Generic

class Base:
	virtual def YieldStuff():
		yield 1
		yield 2
		yield 3

class Derived(Base):
	override def YieldStuff() as IEnumerable of int:
		yield 4
		yield 5
		yield 6

for i in Base().YieldStuff(): print i
for i in Derived().YieldStuff(): print i

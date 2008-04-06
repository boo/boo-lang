"""
foo
"""
import System.Threading

def CAS[of T(class)](ref location as T, compare as T, val as T):
	return Interlocked.CompareExchange[of T](location, val, compare)

s = "foo"
t = "bar"
print CAS[of string](s, s, t)


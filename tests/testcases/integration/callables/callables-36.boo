import NUnit.Framework

def map(fn as callable, iterator):
    return [fn(item) for item in iterator]

def x2(item as int):
    return item*2

Assert.AreEqual([2, 4, 6], map(x2, [1, 2, 3]))

﻿#region license
// Copyright (c) 2003, 2004, Rodrigo B. de Oliveira (rbo@acm.org)
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without modification,
// are permitted provided that the following conditions are met:
// 
//     * Redistributions of source code must retain the above copyright notice,
//     this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice,
//     this list of conditions and the following disclaimer in the documentation
//     and/or other materials provided with the distribution.
//     * Neither the name of Rodrigo B. de Oliveira nor the names of its
//     contributors may be used to endorse or promote products derived from this
//     software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
// THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#endregion


import System
import System.IO
import System.Runtime.Serialization
import System.Runtime.Serialization.Formatters.Binary

class Person:

	[property(FirstName)]
	_fname = ""
	
	[property(LastName)]
	_lname = ""
	
class PersonProxy(Person, ISerializable):

	transient _caboosh = 0
	
	def constructor():
		pass
	
	def constructor(info as SerializationInfo, context as StreamingContext):
		data = info.GetValue("Person.Data", typeof((object)))
		FormatterServices.PopulateObjectMembers(self, GetSerializableMembers(), data)
	
	def GetObjectData(info as SerializationInfo, context as StreamingContext):
		members = GetSerializableMembers()
		info.AddValue("Person.Data", FormatterServices.GetObjectData(self, members))
		
	def GetSerializableMembers():
		return FormatterServices.GetSerializableMembers(Person)
		

def serialize(o):
	stream = MemoryStream()
	BinaryFormatter().Serialize(stream, o)
	return stream.GetBuffer()
	
def deserialize(buffer as (byte)):
	return BinaryFormatter().Deserialize(MemoryStream(buffer))

p = PersonProxy(FirstName: "John", LastName: "Cleese")
p = deserialize(serialize(p))

assert "John" == p.FirstName
assert "Cleese" == p.LastName

	


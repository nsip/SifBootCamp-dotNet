                                README

                  SIFWorks ADK for .NET, AU Edition 
                               Examples

                Copyright ©2001-20108 Edustructures 
                        All rights reserved.

This software is the confidential and proprietary information of
Edustructures LLC ("Confidential Information").  You shall not disclose
such Confidential Information and shall use it only in accordance with 
the terms of the license agreement you entered into with Edustructures.


RUNNING THE EXAMPLES

Each of the examples that ships with the ADK can be run from a .Net  
development environment.


CONNECTING TO ZONES

Some of the examples can be connected to zones using parameters on the
command line. These examples are: SimpleProvider, SimpleSubscriber, and
SIFQuery

Most of the remaining examples (except for SIFQuery) use the standard
ADK XML configuration file for zone connection information. Each of 
these example projects have a file called "Agent.cfg" that can be
edited to connect the example agent to one or more zones. To connect
these agents to a zone, modify or add a <zone> element in the file.
Look for the existing <zone> node in the file and modify it as needed.
Here is an example:

   	<zone id="Zone1" url="http://127.0.0.1:7080/Zone1" /> 


The following example projects are included with the SIFWorks ADK for 
.Net, UK Edition:

CHAMELEON
---------

	The Chameleon example agent is a simple agent that connects to any
	number of zones and requests data from those zones. All query 
	results are stored in XML files in the agent's runtime directory.
	The Chameleon agent can also subscribe to events on objects and
	perform custom queries. Please see the comments in the agent.cfg
	file for Chameleon, that describe the various settings that can
	tailor how the agent works at runtime
	
        RUNNING THE EXAMPLE
	
	To run this example, compile it, update the Zone URL in the 
	agent.cfg, and then run Chameleon.exe


MAPPINGS
--------
	This example shows how to use the powerful mappings subsystem in the
	ADK to dynamically map data to SIF using XPath. The concepts in this
	example are useful as the basis of a production SIF agent.
	
	RUNNING THE EXAMPLE
	
	To run this example, compile it, update the Zone URL in the 
	agent.cfg, and then run Mappings.exe



SIFQUERY
--------
	This example is a simple console application that can be used to 
	connect to a zone and query for data using a simple, SQL-like query 
	syntax.
	
	RUNNING THE EXAMPLE
	
	To run this example, compile it, and use the following syntax:
	SIFQuery.exe /zone Zone1 /url http://localhost:7080/Zone1


SIMPLEPROVIDER
--------------
	This is a very simple agent project that demonstrates how to publish
	data to a SIF zone by responding to SIF_Requests.


	RUNNING THE EXAMPLE
	
	To run this example, compile it, and use the following syntax:
	SimpleProvider.exe /zone Zone1 /url http://localhost:7080/Zone1



SIMPLESUBSCRIBER
----------------
	This is a very simple agent project the demonstrates how to 
	subscribe to SIF Data Objects as well as how to request data of a
	specified type from the zone.

	RUNNING THE EXAMPLE
	
	To run this example, compile it, and use the following syntax:
	SimpleSubscriber.exe /zone Zone1 /url http://localhost:7080/Zone1




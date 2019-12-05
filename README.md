# RabbitMQ.Core  
 The Unofficial Port of the Official RabbitMQ DotNet Client to pure NetCore 3.x  
 
#### NetCore (3.0, 3.1)
[![NuGet](https://img.shields.io/nuget/dt/RabbitMQ.Core.Client.svg)](https://www.nuget.org/packages/RabbitMQ.Core.Client/) [![NuGet](https://img.shields.io/nuget/v/RabbitMQ.Core.Client.svg)](https://www.nuget.org/packages/RabbitMQ.Core.Client/)
 
### What version is it based on?  
v5.1.2

Specifically branch `5.x` from `12/4/2019`.  
Specific Commit: https://github.com/rabbitmq/rabbitmq-dotnet-client/commit/ddbea177be2cbb5834df35992f6abbcd9263571d

### Why this version?
It's pretty hardened. It's got a lot of room for improvement memory allocation wise - but high performance can be maintained through it and it has stood the test of time.  

### Why not v6.0.0?  
 Pivotal's Client team has been working v6.x.x for over a year now (as of 12/5/2019). At this time, 6.0.0 is not close to ready and I don't see them jumping straight into NetCore 3.x either way. I ***will*** add a convert for v6.x.x too, but after it has been properly released (and vetted). The source control will be individually maintained in isolation. It should be super simple and straightforward to see which code you need to look at without jumping through hoops.  

### Versioning Strategy  
Isolated sections will also included new SLNs/Projs/CodeGens/NuGets per major version ported.    
The goal is to modernize, modernize, modernize, while trying to use that same namespace so it can be a drop in replacement.   

### What's different?  
 * Targets NetCoreApp3.0 + 3.1.  
 * Built in C# 8.0.  
 * Various code semantic simplifications made.  
   * Most have been available since C# 6.1+   
 * Roslynator recommendations.   
 * Deleted [Obsolete] or [Deprecated] where it made sense.   
 
 ### How To Build  
 * Open Solution/Folder build.
 
 **Alternatively Run CodeGen**  
 * Before opening solution/project/folder, try running build first for your system.  
 * Run the specific ***.bat*** file for your version.  
 * You should be good to go after than with a clean AMQP API generation.  
 
 ### Linux Support?
 ***Shrugs*** It should?  
 It does compile for Ubunutu... give it a shot and let me know!  
 
`dotnet build --runtime ubuntu.19.04-x64`
 
 ### Pivotal's License
 
```
// This source code is dual-licensed under the Apache License, version
// 2.0, and the Mozilla Public License, version 1.1.
//
// The APL v2.0:
//
//---------------------------------------------------------------------------
//   Copyright (c) 2007-2016 Pivotal Software, Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       https://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//---------------------------------------------------------------------------
//
// The MPL v1.1:
//
//---------------------------------------------------------------------------
//  The contents of this file are subject to the Mozilla Public License
//  Version 1.1 (the "License"); you may not use this file except in
//  compliance with the License. You may obtain a copy of the License
//  at http://www.mozilla.org/MPL/
//
//  Software distributed under the License is distributed on an "AS IS"
//  basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See
//  the License for the specific language governing rights and
//  limitations under the License.
//
//  The Original Code is RabbitMQ.
//
//  The Initial Developer of the Original Code is Pivotal Software, Inc.
//  Copyright (c) 2007-2016 Pivotal Software, Inc.  All rights reserved.
//---------------------------------------------------------------------------
```

### Pivotal's Repo

https://github.com/rabbitmq/rabbitmq-dotnet-client  

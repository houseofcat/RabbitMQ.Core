### What is the library based on?  
Pivotal's RabbitMQ DotNet Client v6.0.0

Specifically branch `master` from `5/9/2020`.  
Specific Commit: https://github.com/rabbitmq/rabbitmq-dotnet-client/commit/c9e36dc3ee8c18f7aa455a228fd44b99d1ba8a57

### What's different?  
 * Targets NetCoreApp3.1.  
 * Builds with C# 8.0/VS2019.  
 * Various code semantic simplifications made.  
   * Most have been available since C# 6.1+   
 * Roslynator recommendations.  
 * Deleted [Obsolete] or [Deprecated] where it made sense.  

### How To Build  
 * Open Solution/Folder build.
 
**Alternatively Run CodeGen**  
 * Before opening solution/project/folder, try running build first for your system.  
 * Run the specific ***.bat*** file for your version.  
 * You should be good to go after that (with a clean AMQP API generation file inside of RabbitMQ.Client folder).  
 
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
//   Copyright (c) 2007-2020 VMware, Inc.
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
//  at https://www.mozilla.org/MPL/
//
//  Software distributed under the License is distributed on an "AS IS"
//  basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See
//  the License for the specific language governing rights and
//  limitations under the License.
//
//  The Original Code is RabbitMQ.
//
//  The Initial Developer of the Original Code is Pivotal Software, Inc.
//  Copyright (c) 2007-2020 VMware, Inc.  All rights reserved.
//---------------------------------------------------------------------------
```

### Pivotal's Repo

https://github.com/rabbitmq/rabbitmq-dotnet-client  

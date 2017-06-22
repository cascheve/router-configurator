using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace BetterRouterProgram
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
    }
}

/*MODULAR SEPARATION OF SCRIPT

Constants:

CWD = os.getcwd()

IMPLICIT_FILES = [
	'boot.ppc',
	'staticRP.cfg',
	'antiacl.cfg',
	'boot.cfg',
	'acl.cfg',
	'xgsn.cfg'
]
===============================

Global Variables:

settings = {
	'username': 'root',
	'port': 'COM1',
	'init_password': '',
	'sys_password': '',
	'ip_addr': '10.1.1.2',
	'router_id': '',
	'router_files_path': './',
}
===============================

Module ProgrammaticUtil*:

+connection_wait() - sleeps
str_to_byte()
+prompt_reboot() - calls function to run instruction
run_macro()
load_variables() - file parsing
+run_instructions() - calls run_instruction()
===============================

Module WindowsUtil:

+connection_wait() - sleeps
start_tftpd()
stop_tftpd()
+load_settings() - file parsing
+run_instructions() - calls run_instruction()
===============================

Module SerialConnectionUtil:

bytes_to_read()
close_connection()
reset_connection_buffers()
read_connection_buffer()
+prompt_reboot() - calls function to run instruction
ping_test()
set_router_time()
set_password()
copy_to_secondary()
copy_files()
run_instruction()
+run_instructions() - calls run_instruction()
router_login()
=============================== */

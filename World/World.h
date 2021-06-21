// The following ifdef block is the standard way of creating macros which make exporting
// from a DLL simpler. All files within this DLL are compiled with the WORLD_EXPORTS
// symbol defined on the command line. This symbol should not be defined on any project
// that uses this DLL. This way any other project whose source files include this file see
// WORLD_API functions as being imported from a DLL, whereas this DLL sees symbols
// defined with this macro as being exported.

#pragma once

#ifdef WORLD_EXPORTS
#define WORLD_API __declspec(dllexport)
#else
#define WORLD_API __declspec(dllimport)
#endif

#ifdef __cplusplus
#define WORLD_BEGIN_C_DECLS extern "C" {
#define WORLD_END_C_DECLS }
#else
#define WORLD_BEGIN_C_DECLS
#define WORLD_END_C_DECLS
#endif

#pragma once

int  native_create_fingerprint(char* pcm_buffer, int pcm_buffer_len, char** out_fp);
void  native_free(void* buffer);


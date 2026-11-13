/* Linux: ライブラリを読み込む前に入れたシグナルハンドラーが、読み込んだあとも残るかを見る */
#include <dlfcn.h>
#include <signal.h>
#include <stdio.h>
#include <string.h>

static void host_handler(int sig, siginfo_t *info, void *ctx)
{
    (void)sig; (void)info; (void)ctx;
}

int main(int argc, char **argv)
{
    static const int sigs[] = { SIGSEGV, SIGBUS, SIGFPE, SIGILL, SIGABRT };
    static const char *names[] = { "SIGSEGV", "SIGBUS", "SIGFPE", "SIGILL", "SIGABRT" };
    const int n = sizeof sigs / sizeof sigs[0];
    struct sigaction sa;

    memset(&sa, 0, sizeof sa);
    sa.sa_sigaction = host_handler;
    sa.sa_flags = SA_SIGINFO;
    for (int k = 0; k < n; k++)
        sigaction(sigs[k], &sa, NULL);

    if (dlopen(argc > 1 ? argv[1] : "./lib/libinspect.so", RTLD_NOW) == NULL) {
        printf("dlopen failed: %s\n", dlerror());
        return 1;
    }

    for (int k = 0; k < n; k++) {
        struct sigaction now;
        sigaction(sigs[k], NULL, &now);
        printf("%-8s %s\n", names[k],
               now.sa_sigaction == host_handler ? "host handler kept" : "replaced");
    }
    return 0;
}

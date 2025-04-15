#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <fcntl.h>
#include <errno.h>
#include <signal.h>


#include "serial.h"



static volatile int keep_running = 1;

void int_handler(int dummy) {
    keep_running = 0;
}



int main(int argc, char *argv[]) {
    char *device = NULL;
    int baudrate = 9600;

    int opt;
    while ((opt = getopt(argc, argv, "d:b:")) != -1) {
        switch (opt) {
            case 'd':
                device = optarg;
                break;
            case 'b':
                baudrate = atoi(optarg);
                break;
            default:
                fprintf(stderr, "Usage: %s -d /dev/ttyUSB0 -b 9600\n", argv[0]);
                exit(EXIT_FAILURE);
        }
    }

    if (!device) {
        fprintf(stderr, "Device path is required. Use -d /dev/ttyUSB0\n");
        return EXIT_FAILURE;
    }


    signal(SIGINT, int_handler);
    signal(SIGTERM, int_handler);
    signal(SIGKILL, int_handler);
    signal(SIGHUP, int_handler);


    int fd = open(device, O_RDWR | O_NOCTTY | O_NONBLOCK);
    if (fd < 0) {
        perror("open");
        return EXIT_FAILURE;
    }

    if (set_interface_attribs(fd, baud_lookup(baudrate)) != 0) {
        close(fd);
        return EXIT_FAILURE;
    }


    fprintf(stderr, "Serial service started on %s @ %d baud\n", device, baudrate);

    char buf[256];


    while (keep_running) {
        fd_set read_fds;
        FD_ZERO(&read_fds);
        FD_SET(STDIN_FILENO, &read_fds);
        FD_SET(fd, &read_fds);

        int max_fd = (STDIN_FILENO > fd ? STDIN_FILENO : fd) + 1;

        int ready = select(max_fd, &read_fds, NULL, NULL, NULL);
        if (ready < 0) {
            if (errno == EINTR) continue; // interrupted by signal
            perror("select");
            break;
        }

        // Check serial port first
        if (FD_ISSET(fd, &read_fds)) {
            char buf[256];
            int n = read(fd, buf, sizeof(buf));
            if (n > 0) {
                fwrite(buf, 1, n, stdout);
                fflush(stdout);
            }
        }

        // Then check stdin
        if (FD_ISSET(STDIN_FILENO, &read_fds)) {
            char buf[256];
            ssize_t len = read(STDIN_FILENO, buf, sizeof(buf));
            if (len > 0) {
                write(fd, buf, len); // send to serial
            }
        }
    }

    fprintf(stderr, "\nShutting down...\n");
    close(fd);
    return 0;
}

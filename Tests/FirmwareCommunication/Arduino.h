#pragma once
#include <cstddef>
#include <cstdint>
#include <cstring>
#include <deque>
#include <vector>
#define F(value) value
long random();

class TestSerial
{
public:
    std::deque<uint8_t> input;
    std::vector<uint8_t> output;
    void begin(uint32_t) {}
    void setTimeout(uint64_t) {}
    int available() { return input.size(); }
    int read() { auto value = input.front(); input.pop_front(); return value; }
    size_t readBytes(char *buffer, size_t count)
    {
        size_t read = 0;
        while (read < count && !input.empty()) buffer[read++] = this->read();
        return read;
    }
    void write(uint8_t value) { output.push_back(value); }
    void write(const char *data, size_t count) { output.insert(output.end(), data, data + count); }
    void println(const char *data) { write(data, strlen(data)); write('\r'); write('\n'); }
    void flush() {}
};
extern TestSerial Serial;

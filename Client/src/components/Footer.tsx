import { Box, BoxProps, Button, IconButton, Stack, Text, useColorModeValue, VStack } from '@chakra-ui/react';
import { FaGithub } from 'react-icons/fa';
import { AppLogo } from './AppLogo.tsx';
import { Link } from 'react-router-dom';

export const Footer = (props: BoxProps) => {
    return (
        <Box
            as="footer"
            role="contentinfo"
            {...props}
            bg="bg.accent.default"
            color={useColorModeValue('gray.700', 'gray.200')}
        >
            <Box
                as={Stack}
                py={4}
                px={4}
                direction={{ base: 'column', md: 'row' }}
                spacing={4}
                justify={{ base: 'center', md: 'space-between' }}
                align={{ base: 'center', md: 'center' }}
            >
                <AppLogo size="small" />
                <VStack align="start" spacing={0}>
                    <Text>© {new Date().getFullYear()} Tech Gadget. All rights reserved.</Text>
                    <Text color={useColorModeValue('gray.600', 'gray.400')}>
                        Built with React, ASP.NET Core Web API, and PostgreSQL. (
                        <Button
                            variant="link"
                            colorScheme="blue"
                            as={Link}
                            target="_blank"
                            to="https://github.com/minsoeaung/tech-gadget"
                        >
                            View source
                        </Button>
                        )
                    </Text>
                </VStack>
                <Stack direction={'row'} spacing={6}>
                    <IconButton
                        as={Link}
                        to="https://github.com/minsoeaung/tech-gadget"
                        target="_blank"
                        isRound
                        variant="solid"
                        colorScheme="linkedin"
                        aria-label="Github link"
                        icon={<FaGithub />}
                    />
                </Stack>
            </Box>
        </Box>
    );
};

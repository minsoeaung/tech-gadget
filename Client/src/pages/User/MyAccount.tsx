import {useMyAccount} from "../../hooks/queries/useMyAccount.ts";
import {
    Avatar,
    AvatarBadge,
    Button,
    Card,
    Container,
    Heading,
    HStack,
    IconButton,
    Input,
    Tag,
    Text,
    VStack
} from "@chakra-ui/react";
import {CheckIcon, EditIcon} from "@chakra-ui/icons";
import AntdSpin from "../../components/AntdSpin";

const MyAccount = () => {
    const {data, isLoading, isError} = useMyAccount();

    if (isLoading) {
        return <AntdSpin/>
    }

    if (isError) {
        return <p>Error loading account settings</p>
    }

    if (!data) return null;

    return (
        <Container maxW="7xl">
            <Card variant="outline">
                <VStack p={{base: 6, lg: 10}} alignItems="start" spacing={{base: 6, lg: 10}}>
                    <Heading lineHeight={1.1} fontSize={{base: '2xl', sm: '3xl'}}>
                        My Account
                    </Heading>
                    <HStack spacing={{base: 2, md: 6}}>
                        <Avatar size="xl" src="https://bit.ly/sage-adebayo">
                            <AvatarBadge
                                as={IconButton}
                                size="sm"
                                rounded="full"
                                top="-10px"
                                colorScheme="blue"
                                aria-label="Modify Image"
                                icon={<EditIcon/>}
                            />
                        </Avatar>

                        <VStack alignItems="start">
                            <Input
                                defaultValue={data.userName}
                                placeholder="UserName"
                                _placeholder={{color: 'gray.500'}}
                                type="text"
                                isDisabled
                            />
                            {!(data.roles.length === 1 && data.roles.includes("User")) && (
                                <HStack>
                                    <Text fontSize="sm">Account type: </Text>
                                    {data.roles.map(role => (
                                        <Tag colorScheme="blue" key={role}>{role}</Tag>
                                    ))}
                                </HStack>
                            )}
                        </VStack>
                    </HStack>
                    <VStack align="start">
                        <HStack>
                            <Text>Email</Text>
                            {data.emailConfirmed ? (
                                <Button leftIcon={<CheckIcon/>} size="xs" variant="ghost" colorScheme="green"
                                        isDisabled>
                                    Verified
                                </Button>
                            ) : (
                                <Button size="xs" variant="outline" isDisabled>
                                    Verify
                                </Button>
                            )}
                        </HStack>
                        <HStack>
                            <Text color={'gray.500'}>{data.email}</Text>
                            <IconButton
                                size="xs"
                                variant="ghost"
                                aria-label="Change email"
                                icon={<EditIcon/>}
                            />
                        </HStack>
                    </VStack>
                    <VStack align="start">
                        <HStack>
                            <Text>Phone</Text>
                            {!!data.phoneNumber && (
                                data.phoneNumber ? (
                                    <Button leftIcon={<CheckIcon/>} size="xs" variant="ghost" colorScheme="green"
                                            isDisabled>
                                        Verified
                                    </Button>
                                ) : (
                                    <Button size="xs" variant="outline" isDisabled>
                                        Verify
                                    </Button>
                                )
                            )}
                        </HStack>
                        <HStack>
                            {data.phoneNumber ? (
                                <>
                                    <Text color={'gray.500'}>{data.phoneNumber}</Text>
                                    <IconButton
                                        size="xs"
                                        variant="ghost"
                                        aria-label="Change email"
                                        icon={<EditIcon/>}
                                    />
                                </>

                            ) : (
                                <Button size="xs" variant="outline" isDisabled>
                                    Add phone
                                </Button>
                            )}
                        </HStack>
                    </VStack>
                </VStack>
            </Card>
        </Container>
    )
};

export default MyAccount;